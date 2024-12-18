using BepInEx;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using Valve.VR;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;

namespace FortniteEmoteWheel.Classes
{
    public class Wheel : MonoBehaviour
    {
        public static Wheel instance;

        private GameObject Base;
        private GameObject Selector;

        private void Awake()
        {
            instance = this;
            Base = transform.Find("Base").gameObject;
            Selector = Base.transform.Find("Selected").gameObject;

            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            Base.SetActive(visible);
        }

        public float GetTurnAngle(Vector2 position)
        {
            if (position.magnitude < 0.01f)
                return 90;

            float angle = Mathf.Atan2(position.y, position.x) * Mathf.Rad2Deg;

            return angle;
        }

        private float changePageDelay = 0f;
        private float prevRotation = -9999f;
        private int Selection = 0;
        private int Page = 0;
        public void Update()
        {
            Vector2 Direction = UnityInput.Current.GetKey(KeyCode.B) ? -new Vector2(Screen.width / 2f - Mouse.current.position.x.value, Screen.height / 2f - Mouse.current.position.y.value).normalized : SteamVR_Actions.gorillaTag_RightJoystick2DAxis.axis;

            if (SteamVR_Actions.gorillaTag_LeftJoystickClick.state || UnityInput.Current.GetKey(KeyCode.V))
            {
                Plugin.emoteTime = -9999f;

                if (Plugin.audiomgr != null)
                    Plugin.audiomgr.GetComponent<AudioSource>().Stop();
            }

            if ((Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed || Mathf.Abs(SteamVR_Actions.gorillaTag_LeftJoystick2DAxis.axis.x) > 0.5f) && Time.time > changePageDelay && Base.activeSelf)
            {
                changePageDelay = Time.time + 0.15f;
                int lastPage = 2;

                Plugin.Play2DAudio(Plugin.LoadSoundFromResource("nav"), 0.5f);

                Page += (SteamVR_Actions.gorillaTag_LeftJoystick2DAxis.axis.x > 0.5f || Mouse.current.rightButton.isPressed ? 1 : -1);
                if (Page < 0)
                    Page = lastPage;

                if (Page > lastPage)
                    Page = 0;

                GameObject Pages = Base.transform.Find("Pages").gameObject;
                for (int i = 0; i < Pages.transform.childCount; i++)
                {
                    Pages.transform.GetChild(i).gameObject.SetActive(int.Parse(Pages.transform.GetChild(i).name) == Page);
                }
            }

            if (SteamVR_Actions.gorillaTag_RightJoystickClick.state || UnityInput.Current.GetKey(KeyCode.B))
            {
                if (Time.time > Plugin.emoteTime)
                {
                    GameObject.Find("Player Objects/Player VR Controller/GorillaPlayer").GetComponent<GorillaSnapTurn>().enabled = false;
                    SetVisible(true);
                }

                if (Base.activeSelf)
                {
                    if (UnityInput.Current.GetKey(KeyCode.B))
                    {
                        Base.transform.position = GorillaTagger.Instance.headCollider.transform.position + GorillaTagger.Instance.headCollider.transform.forward;
                        Base.transform.rotation = GorillaTagger.Instance.headCollider.transform.rotation * Quaternion.Euler(0f, 0f, 180f);
                    }

                    Selector.transform.localRotation = Quaternion.Euler(0f, 0f, 45 * Mathf.Round((GetTurnAngle(Direction) - 90f) / 45f));
                    
                    if (Selector.transform.localRotation.z != prevRotation)
                    {
                        Plugin.Play2DAudio(Plugin.LoadSoundFromResource("nav"), 0.5f);
                    }
                    prevRotation = Selector.transform.localRotation.z;

                    int selected = (int)Math.Floor(Math.Round((GetTurnAngle(Direction) - 90f) / 45f));
                    Selection = selected;

                    string pageTitle = "";
                    string emoteTitle = "";

                    if (Page == 0)
                    {
                        pageTitle = "ORIGINAL 1";
                        switch (selected)
                        {
                            case 0:
                                emoteTitle = "DANCE MOVES";
                                break;
                            case -1:
                                emoteTitle = "TAKE THE L";
                                break;
                            case -2:
                                emoteTitle = "REANIMATED";
                                break;
                            case -3:
                                emoteTitle = "ELECTRO SHUFFLE";
                                break;
                            case -4:
                                emoteTitle = "ORANGE JUSTICE";
                                break;
                            case -5:
                                emoteTitle = "RIDE THE PONY";
                                break;
                            case -6:
                            case 2:
                                emoteTitle = "FRESH";
                                break;
                            case 1:
                                emoteTitle = "ELECTRO SWING";
                                break;
                        }
                    }
                    if (Page == 1)
                    {
                        pageTitle = "ORIGINAL 2";
                        switch (selected)
                        {
                            case 0:
                                emoteTitle = "FLOSS";
                                break;
                            case -1:
                                emoteTitle = "DISCO FEVER";
                                break;
                        }
                    }
                    if (Page == 2)
                    {
                        pageTitle = "NEW";
                        switch (selected)
                        {
                            case 0:
                                emoteTitle = "PAWS & CLAWS";
                                break;
                            case -1:
                                emoteTitle = "GET GRIDDY";
                                break;
                            case -2:
                                emoteTitle = "PULL UP";
                                break;
                            case -3:
                                emoteTitle = "POPULAR VIBE";
                                break;
                            case -4:
                                emoteTitle = "LUCID DREAMS";
                                break;
                            case -5:
                                emoteTitle = "EMPTY OUT YOUR POCKETS";
                                break;
                            case -6:
                            case 2:
                                emoteTitle = "WHAT YOU WANT";
                                break;
                        }
                    }

                    Base.transform.Find("Canvas/PageName").GetComponent<Text>().text = pageTitle;
                    Base.transform.Find("Canvas/EmoteName").GetComponent<Text>().text = emoteTitle;
                }
            }
            else
            {
                if (Base.activeSelf)
                {
                    GameObject.Find("Player Objects/Player VR Controller/GorillaPlayer").GetComponent<GorillaSnapTurn>().enabled = true;
                    SetVisible(false);

                    if (Page == 0)
                    {
                        switch (Selection)
                        {
                            case 0:
                                Plugin.Emote("Dance Moves", "default");
                                break;
                            case -1:
                                GorillaTagger.Instance.offlineVRRig.rightMiddle.calcT = 1f;
                                GorillaTagger.Instance.offlineVRRig.rightMiddle.LerpFinger(1f, false);
                                Plugin.Emote("TakeTheL", "takethel", -1f, true);
                                break;
                            case -2:
                                Plugin.Emote("Reanimated", "reanimated", -1f, true);
                                break;
                            case -3:
                                Plugin.Emote("ElectroShuffle", "electroshuffle", -1f, true);
                                break;
                            case -4:
                                Plugin.Emote("OrangeJustice", "oj", -1f, true);
                                break;
                            case -5:
                                Plugin.Emote("RideThePony", "ridethepony", -1f, true);
                                break;
                            case -6:
                            case 2:
                                Plugin.Emote("Emote_Fresh", "fresh", -1f, true);
                                break;
                            case 1:
                                Plugin.Emote("ElectroSwing", "swing", -1f, true);
                                break;
                        }
                    }
                    if (Page == 1)
                    {
                        switch (Selection)
                        {
                            case 0:
                                GorillaTagger.Instance.offlineVRRig.leftIndex.calcT = 1f;
                                GorillaTagger.Instance.offlineVRRig.leftMiddle.calcT = 1f;

                                GorillaTagger.Instance.offlineVRRig.rightIndex.calcT = 1f;
                                GorillaTagger.Instance.offlineVRRig.rightMiddle.calcT = 1f;

                                GorillaTagger.Instance.offlineVRRig.rightIndex.LerpFinger(1f, false);
                                GorillaTagger.Instance.offlineVRRig.rightMiddle.LerpFinger(1f, false);

                                GorillaTagger.Instance.offlineVRRig.leftIndex.LerpFinger(1f, false);
                                GorillaTagger.Instance.offlineVRRig.leftMiddle.LerpFinger(1f, false);

                                Plugin.Emote("Emote_FlossDance_CMM", "floss", -1f, true);
                                break;
                            case -1:
                                GorillaTagger.Instance.offlineVRRig.leftMiddle.calcT = 1f;
                                GorillaTagger.Instance.offlineVRRig.rightMiddle.calcT = 1f;

                                GorillaTagger.Instance.offlineVRRig.rightMiddle.LerpFinger(1f, false);
                                GorillaTagger.Instance.offlineVRRig.leftMiddle.LerpFinger(1f, false);

                                Plugin.Emote("DiscoFever", "discofever", -1f, true);
                                break;
                        }
                    }
                    if (Page == 2)
                    {
                        switch (Selection)
                        {
                            case 0:
                                Plugin.Emote("Paws&Claws", "pawsclaws", -1f, true);
                                break;
                            case -1:
                                Plugin.Emote("Get Griddy", "Emote_Griddles_Music_Loop_01", -1f, true);
                                break;
                            case -2:
                                Plugin.Emote("Pull Up", "Gas_Station_Loop", -1f, true);
                                break;
                            case -3:
                                Plugin.Emote("Popular Vibe", "Emote_SpeedDial_Loop", -1f, true);
                                break;
                            case -4:
                                Plugin.Emote("Lucid DreamsLoop", "Emote_KelpLinen_Music_Loop", -1f, true);
                                break;
                            case -5:
                                Plugin.Emote("Empty Out Your PocketsLoop", "eoyp", -1f, true);
                                break;
                            case -6:
                            case 2:
                                Plugin.Emote("WhatYouWant", "whatyouwant", -1f, true);
                                break;
                        }
                    }
                    prevRotation = -9999f;
                }
            }
        }
    }
}
