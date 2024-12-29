using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace FortniteEmoteWheel
{
    public class Networking_Callbacks : MonoBehaviourPunCallbacks
    {
        public string[] ids = new string[]
        {
            "970B338BBDC11A77",// me quest
            "9BE103424DF13F2E",// me steam
            "CA8FDFF42B7A1836",// broken stone told me to add them
            "4994748F8B361E31",// eve also asked
            "3CB4F61C87A5AF24"// eve's other account
        };
        
        public override void OnJoinedRoom()
        {
            PhotonNetwork.LocalPlayer.CustomProperties.Add("EmoteMod", true);
            PhotonNetwork.LocalPlayer.SetCustomProperties(PhotonNetwork.LocalPlayer.CustomProperties);
            foreach (Player player in PhotonNetwork.PlayerListOthers)
            {
                if (ids.Contains(player.UserId))
                {
                    StartCoroutine(thingidk());
                }
            }
        }

        public override void OnPlayerEnteredRoom(Player player)
        {
            if (ids.Contains(player.UserId))
            {
                StartCoroutine(thingidk());
            }
        }

        public IEnumerator thingidk()
        {
            yield return new WaitForSecondsRealtime(2);
            NetworkSystem.Instance.ReturnToSinglePlayer();
        }
    }
}