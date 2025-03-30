using BepInEx;
using GorillaTag.CosmeticSystem;
using Photon.Pun;
using Photon.Voice.Unity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace FortniteEmoteWheel
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public void Start()
        {
            HarmonyPatches.ApplyHarmonyPatches();
        }

        private static AssetBundle assetBundle;
        public static GameObject LoadAsset(string assetName)
        {
            GameObject gameObject = null;

            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FortniteEmoteWheel.Resources.fn");
            if (stream != null)
            {
                if (assetBundle == null)
                {
                    assetBundle = AssetBundle.LoadFromStream(stream);
                }
                gameObject = Instantiate<GameObject>(assetBundle.LoadAsset<GameObject>(assetName));
            }
            else
            {
                Debug.LogError("Failed to load asset from resource: " + assetName);
            }

            return gameObject;
        }

        public static GameObject audiomgr = null;
        public static void Play2DAudio(AudioClip sound, float volume, bool looping = false)
        {
            if (audiomgr == null)
            {
                audiomgr = new GameObject("2DAudioMgr");
                AudioSource temp = audiomgr.AddComponent<AudioSource>();
                temp.spatialBlend = 0f;
            }
            AudioSource ausrc = audiomgr.GetComponent<AudioSource>();
            ausrc.volume = volume;
            ausrc.loop = looping;
            if (!looping)
                ausrc.PlayOneShot(sound);
            else
            {
                ausrc.clip = sound;
                ausrc.Play();
            }
        }

        public static Dictionary<string, AudioClip> audioPool = new Dictionary<string, AudioClip> { };
        public static AudioClip LoadSoundFromResource(string resourcePath)
        {
            AudioClip sound = null;

            if (!audioPool.ContainsKey(resourcePath))
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FortniteEmoteWheel.Resources.fn");
                if (stream != null)
                {
                    if (assetBundle == null)
                    {
                        assetBundle = AssetBundle.LoadFromStream(stream);
                    }
                    sound = assetBundle.LoadAsset(resourcePath) as AudioClip;
                    audioPool.Add(resourcePath, sound);
                }
                else
                {
                    Debug.LogError("Failed to load sound from resource: " + resourcePath);
                }
            }
            else
            {
                sound = audioPool[resourcePath];
            }

            return sound;
        }

        private static List<GameObject> portedCosmetics = new List<GameObject> { };
        public static void DisableCosmetics()
        {
            try
            {
                GorillaTagger.Instance.offlineVRRig.transform.Find("RigAnchor/rig/body/head/gorillaface").gameObject.layer = LayerMask.NameToLayer("Default");
                foreach (GameObject Cosmetic in GorillaTagger.Instance.offlineVRRig.cosmetics)
                {
                    if (Cosmetic.activeSelf && Cosmetic.transform.parent == GorillaTagger.Instance.offlineVRRig.mainCamera.transform)
                    {
                        portedCosmetics.Add(Cosmetic);
                        Cosmetic.transform.SetParent(GorillaTagger.Instance.offlineVRRig.headMesh.transform, false);
                        Cosmetic.transform.localPosition += new Vector3(0f, 0.1333f, 0.1f);
                    }
                }
            } catch { }
        }

        public static void EnableCosmetics()
        {
            GorillaTagger.Instance.offlineVRRig.transform.Find("RigAnchor/rig/body/head/gorillaface").gameObject.layer = LayerMask.NameToLayer("MirrorOnly");
            foreach (GameObject Cosmetic in portedCosmetics)
            {
                Cosmetic.transform.SetParent(GorillaTagger.Instance.offlineVRRig.mainCamera.transform, false);
                Cosmetic.transform.localPosition -= new Vector3(0f, 0.1333f, 0.1f);
            }
            portedCosmetics.Clear();
        }

        public static GameObject Kyle;
        public static float emoteTime;

        private static int PreviousSerializationRate = -1;

        public static Vector3 archivePosition;

        public static void Emote(string emoteName, string emoteSound, float animationTime = -1f, bool looping = false)
        {
            if (Kyle != null)
                UnityEngine.Object.Destroy(Kyle);

            GorillaTagger.Instance.offlineVRRig.enabled = false;
            DisableCosmetics();

            PreviousSerializationRate = PhotonNetwork.SerializationRate;
            PhotonNetwork.SerializationRate *= 3;

            Play2DAudio(LoadSoundFromResource("play"), 0.5f);

            archivePosition = GorillaTagger.Instance.transform.position;
            GorillaLocomotion.GTPlayer.Instance.rightControllerTransform.parent.rotation *= Quaternion.Euler(0f, 180f, 0f);

            Kyle = LoadAsset("Rig"); 
            Kyle.transform.position = GorillaTagger.Instance.offlineVRRig.transform.Find("RigAnchor/rig/body").position - new Vector3(0f, 1.15f, 0f);
            Kyle.transform.rotation = GorillaTagger.Instance.offlineVRRig.transform.Find("RigAnchor/rig/body").rotation;

            Kyle.transform.Find("KyleRobot/RobotKile").gameObject.GetComponent<Renderer>().renderingLayerMask = 0;

            Animator KyleRobot = Kyle.transform.Find("KyleRobot").GetComponent<Animator>();
            KyleRobot.enabled = true;
            
            AnimationClip Animation = null;
            foreach (AnimationClip Clip in KyleRobot.runtimeAnimatorController.animationClips)
            {
                if (Clip.name == emoteName)
                {
                    Animation = Clip;
                    break;
                }
            }

            Animation.wrapMode = looping ? WrapMode.Loop : WrapMode.Default;
            KyleRobot.Play(Animation.name);

            AudioClip Sound = LoadSoundFromResource(emoteSound);
            Play2DAudio(Sound, 0.5f, looping);

            if (GorillaTagger.Instance.myRecorder != null)
            {
                GorillaTagger.Instance.myRecorder.SourceType = Recorder.InputSourceType.AudioClip;
                GorillaTagger.Instance.myRecorder.AudioClip = Sound;
                GorillaTagger.Instance.myRecorder.RestartRecording(true);
            }

            emoteTime = Time.time + (animationTime > 0f ? animationTime : Animation.length) + (looping ? 999999999999999f : 0);
        }

        public static Vector3 World2Player(Vector3 world)
        {
            return world - GorillaTagger.Instance.bodyCollider.transform.position + GorillaTagger.Instance.transform.position;
        }

        public void Update()
        {
            if (Classes.Wheel.instance == null && GorillaTagger.Instance.offlineVRRig != null)
            {
                GameObject Wheel = Plugin.LoadAsset("Wheel");
                Wheel.transform.SetParent(GorillaTagger.Instance.offlineVRRig.transform.Find("RigAnchor/rig/body/shoulder.R/upper_arm.R/forearm.R/hand.R"), false);
                Wheel.AddComponent<Classes.Wheel>();
            }

            if (Time.time < emoteTime)
            {
                if (Kyle != null)
                {
                    GorillaTagger.Instance.offlineVRRig.enabled = false;

                    GorillaTagger.Instance.transform.position = World2Player(Kyle.transform.position + (Kyle.transform.forward * 1.5f) + new Vector3(0f, 1.15f, 0f)) + new Vector3(0f, 0.5f, 0f);
                    GorillaTagger.Instance.leftHandTransform.position = GorillaTagger.Instance.bodyCollider.transform.position;
                    GorillaTagger.Instance.rightHandTransform.position = GorillaTagger.Instance.bodyCollider.transform.position;

                    GorillaTagger.Instance.rigidbody.velocity = Vector3.zero;

                    GorillaTagger.Instance.offlineVRRig.transform.position = Kyle.transform.Find("KyleRobot/ROOT/Hips/Spine1/Spine2").transform.position - (Kyle.transform.Find("KyleRobot/ROOT/Hips/Spine1/Spine2").transform.right / 2.5f);
                    GorillaTagger.Instance.offlineVRRig.transform.rotation = Quaternion.Euler(new Vector3(0f, Kyle.transform.Find("KyleRobot/ROOT/Hips/Spine1/Spine2").transform.rotation.eulerAngles.y, 0f));

                    GorillaTagger.Instance.offlineVRRig.leftHand.rigTarget.transform.position = Kyle.transform.Find("KyleRobot/ROOT/Hips/Spine1/Spine2/LeftShoulder/LeftUpperArm/LeftArm/LeftHand").transform.position;
                    GorillaTagger.Instance.offlineVRRig.rightHand.rigTarget.transform.position = Kyle.transform.Find("KyleRobot/ROOT/Hips/Spine1/Spine2/RightShoulder/RightUpperArm/RightArm/RightHand").transform.position;

                    GorillaTagger.Instance.offlineVRRig.leftHand.rigTarget.transform.rotation = Kyle.transform.Find("KyleRobot/ROOT/Hips/Spine1/Spine2/LeftShoulder/LeftUpperArm/LeftArm/LeftHand").transform.rotation * Quaternion.Euler(0, 0, 75);
                    GorillaTagger.Instance.offlineVRRig.rightHand.rigTarget.transform.rotation = Kyle.transform.Find("KyleRobot/ROOT/Hips/Spine1/Spine2/RightShoulder/RightUpperArm/RightArm/RightHand").transform.rotation * Quaternion.Euler(180, 0, -75);

                    GorillaTagger.Instance.offlineVRRig.head.rigTarget.transform.rotation = Kyle.transform.Find("KyleRobot/ROOT/Hips/Spine1/Spine2/Neck/Head").transform.rotation * Quaternion.Euler(0f, 0f, 90f);
                }
            } else
            {
                if (Kyle != null)
                {
                    GorillaTagger.Instance.offlineVRRig.enabled = true;
                    EnableCosmetics();

                    if (PreviousSerializationRate > 0)
                        PhotonNetwork.SerializationRate = PreviousSerializationRate;

                    UnityEngine.Object.Destroy(Kyle);

                    if (GorillaTagger.Instance.myRecorder != null)
                    {
                        GorillaTagger.Instance.myRecorder.SourceType = Recorder.InputSourceType.Microphone;
                        GorillaTagger.Instance.myRecorder.AudioClip = null;
                        GorillaTagger.Instance.myRecorder.RestartRecording(true);
                    }

                    GorillaTagger.Instance.transform.position = archivePosition;
                    GorillaLocomotion.GTPlayer.Instance.rightControllerTransform.parent.rotation *= Quaternion.Euler(0f, 180f, 0f);
                }
            }
        }
    }
}
