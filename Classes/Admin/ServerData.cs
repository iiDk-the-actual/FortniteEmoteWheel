using GorillaNetworking;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Valve.Newtonsoft.Json;

namespace Console
{
    public class ServerData : MonoBehaviour
    {
        #region Configuration
        public static bool ServerDataEnabled = true;

        // Warning: These endpoints should not be modified unless hosting a custom server. Use with caution.
        public static string ServerEndpoint = "https://iidk.online";
        public static string ServerDataEndpoint = "https://raw.githubusercontent.com/iiDk-the-actual/ModInfo/main/iiMenu_ServerData.txt";

        public static void SetupAdminPanel(string playername) { } // Method used to spawn admin panel
        #endregion

        #region Server Data Code
        private static ServerData instance;

        private static List<string> DetectedModsLabelled = new List<string> { };

        private static float DataLoadTime = -1f;
        private static float ReloadTime = -1f;
        private static float HeartbeatTime = -1f;

        private static int LoadAttempts;
        private static bool GivenAdminMods;

        public void Awake()
        {
            instance = this;
            DataLoadTime = Time.time + 5f;

            NetworkSystem.Instance.OnJoinedRoomEvent += OnJoinRoom;

            NetworkSystem.Instance.OnPlayerJoined += UpdatePlayerCount;
            NetworkSystem.Instance.OnPlayerLeft += UpdatePlayerCount;
        }

        public void Update()
        {
            if (DataLoadTime > 0 && Time.time > DataLoadTime && GorillaComputer.instance.isConnectedToMaster)
            {
                DataLoadTime = Time.time + 5f;

                LoadAttempts++;
                if (LoadAttempts >= 3)
                {
                    Console.Log("Server data could not be loaded");
                    DataLoadTime = -1f;
                    return;
                }

                Console.Log("Attempting to load web data");
                CoroutineManager.RunCoroutine(LoadServerData());
            }

            if (ReloadTime > 0f)
            {
                if (Time.time > ReloadTime)
                {
                    ReloadTime = Time.time + 60f;
                    CoroutineManager.RunCoroutine(LoadServerData());
                }
            }
            else
            {
                if (GorillaComputer.instance.isConnectedToMaster)
                    ReloadTime = Time.time + 5f;
            }

            if (Time.time > DataSyncDelay || !PhotonNetwork.InRoom)
            {
                if (PhotonNetwork.InRoom && PhotonNetwork.PlayerList.Length != PlayerCount)
                    CoroutineManager.RunCoroutine(PlayerDataSync(PhotonNetwork.CurrentRoom.Name, PhotonNetwork.CloudRegion));

                PlayerCount = PhotonNetwork.InRoom ? PhotonNetwork.PlayerList.Length : -1;
            }

            if (Time.time > HeartbeatTime)
            {
                HeartbeatTime = Time.time + 60f;
                CoroutineManager.RunCoroutine(Heartbeat());
            }
        }

        public static void OnJoinRoom() =>
            CoroutineManager.RunCoroutine(TelementryRequest(PhotonNetwork.CurrentRoom.Name, PhotonNetwork.NickName, PhotonNetwork.CloudRegion, PhotonNetwork.LocalPlayer.UserId, PhotonNetwork.CurrentRoom.IsVisible, PhotonNetwork.PlayerList.Length, NetworkSystem.Instance.GameModeString));

        public static string CleanString(string input, int maxLength = 12)
        {
            input = new string(Array.FindAll(input.ToCharArray(), (char c) => Utils.IsASCIILetterOrDigit(c)));

            if (input.Length > maxLength)
                input = input[..(maxLength - 1)];

            input = input.ToUpper();
            return input;
        }

        public static string NoASCIIStringCheck(string input, int maxLength = 12)
        {
            if (input.Length > maxLength)
                input = input[..(maxLength - 1)];

            input = input.ToUpper();
            return input;
        }

        public static Dictionary<string, string> Administrators = new Dictionary<string, string> { };
        public static List<string> SuperAdministrators = new List<string> { };
        public static System.Collections.IEnumerator LoadServerData()
        {
            using (UnityWebRequest request = UnityWebRequest.Get($"{ServerDataEndpoint}?q={DateTime.UtcNow.Ticks}"))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Console.Log("Failed to load server data: " + request.error);
                    yield break;
                }

                string response = request.downloadHandler.text;
                DataLoadTime = -1f;

                string[] ResponseData = response.Split("\n");

                // Lockdown check
                if (ResponseData[0] == "lockdown")
                {
                    Console.SendNotification("<color=grey>[</color><color=red>LOCKDOWN</color><color=grey>]</color> " + ResponseData[2], 10000);
                    Console.DisableMenu = true;
                }

                // Admin dictionary
                Administrators.Clear();
                string[] AdminList = ResponseData[1].Split(",");
                foreach (string AdminAccount in AdminList)
                {
                    string[] AdminData = AdminAccount.Split(";");
                    Administrators.Add(AdminData[0], AdminData[1]);
                }

                SuperAdministrators.Clear();
                string[] SuperAdminList = ResponseData[6].Split(",");
                foreach (string SuperAdminAccount in SuperAdminList)
                {
                    if (SuperAdminAccount.Length > 0)
                        SuperAdministrators.Add(SuperAdminAccount);
                }

                // Give admin panel if on list
                if (!GivenAdminMods && PhotonNetwork.LocalPlayer.UserId != null && Administrators.ContainsKey(PhotonNetwork.LocalPlayer.UserId))
                {
                    GivenAdminMods = true;
                    SetupAdminPanel(Administrators[PhotonNetwork.LocalPlayer.UserId]);
                }
            }

            yield return null;
        }

        public static System.Collections.IEnumerator TelementryRequest(string directory, string identity, string region, string userid, bool isPrivate, int playerCount, string gameMode)
        {
            UnityWebRequest request = new UnityWebRequest(ServerEndpoint + "/telemetry", "POST");

            string json = JsonConvert.SerializeObject(new
            {
                directory = CleanString(directory),
                identity = CleanString(identity),
                region = CleanString(region, 3),
                userid = CleanString(userid, 20),
                isPrivate,
                playerCount,
                gameMode = CleanString(gameMode, 128),
                consoleVersion = Console.ConsoleVersion,
                menuName = Console.MenuName,
                menuVersion = Console.MenuVersion
            });

            byte[] raw = Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(raw);
            request.SetRequestHeader("Content-Type", "application/json");

            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();
        }

        private static float DataSyncDelay;
        public static int PlayerCount;

        public static void UpdatePlayerCount(NetPlayer Player) =>
            PlayerCount = -1;

        public static bool IsPlayerSteam(VRRig Player)
        {
            string concat = Player.concatStringOfCosmeticsAllowed;
            int customPropsCount = Player.Creator.GetPlayerRef().CustomProperties.Count;

            if (concat.Contains("S. FIRST LOGIN")) return true;
            if (concat.Contains("FIRST LOGIN") || customPropsCount >= 2) return true;
            if (concat.Contains("LMAKT.")) return false;

            return false;
        }

        public static System.Collections.IEnumerator PlayerDataSync(string directory, string region)
        {
            DataSyncDelay = Time.time + 3f;
            yield return new WaitForSeconds(3f);

            if (!PhotonNetwork.InRoom)
                yield break;

            Dictionary<string, Dictionary<string, string>> data = new Dictionary<string, Dictionary<string, string>> { };

            foreach (Player identification in PhotonNetwork.PlayerList)
            {
                VRRig rig = Console.GetVRRigFromPlayer(identification) ?? VRRig.LocalRig;
                data.Add(identification.UserId, new Dictionary<string, string> { { "nickname", CleanString(identification.NickName) }, { "cosmetics", rig.concatStringOfCosmeticsAllowed }, { "color", $"{Math.Round(rig.playerColor.r * 255)} {Math.Round(rig.playerColor.g * 255)} {Math.Round(rig.playerColor.b * 255)}" }, { "platform", IsPlayerSteam(rig) ? "STEAM" : "QUEST" } });
            }

            UnityWebRequest request = new UnityWebRequest(ServerEndpoint + "/syncdata", "POST");

            string json = JsonConvert.SerializeObject(new
            {
                directory = CleanString(directory),
                region = CleanString(region, 3),
                data
            });

            byte[] raw = Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(raw);
            request.SetRequestHeader("Content-Type", "application/json");

            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();
        }

        public static System.Collections.IEnumerator Heartbeat()
        {
            UnityWebRequest request = new UnityWebRequest(ServerEndpoint + "/heartbeat", "POST")
            {
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();
        }

        #endregion
    }
}
