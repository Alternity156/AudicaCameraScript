using System;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using OVRSimpleJSON;
using System.IO;
using Harmony;

namespace AudicaModding
{
    public class AudicaMod : MelonMod
    {
        public static class BuildInfo
        {
            public const string Name = "CameraScript";  // Name of the Mod.  (MUST BE SET)
            public const string Author = "Alternity"; // Author of the Mod.  (Set as null if none)
            public const string Company = null; // Company that made the Mod.  (Set as null if none)
            public const string Version = "0.1.1"; // Version of the Mod.  (MUST BE SET)
            public const string DownloadLink = null; // Download Link for the Mod.  (Set as null if none)
        }

        public static Vector3 debugTextPos = new Vector3(0f, -1f, 5f);

        public static bool camOK = false;
        public static bool isMouseAwake = false;
        public static bool scriptExists = false;

        public static SpectatorCam spectatorCam;

        public static float mouseSensitivity = 100.0f;

        public static float xAxis;
        public static float yAxis;

        public static string dir = Application.dataPath + "/../Mods/Config/CameraScript/";

        //The current way of tracking menu state.
        //TODO: Hook to the SetMenuState function without breaking the game
        public static MenuState.State menuState;
        public static MenuState.State oldMenuState;

        public static string selectedSong;
        public static float fovSetting;

        public static CameraCue[] cameraCues;
        public static float tempFov;

        public static float lastTick;
        public static float oldLastTick;
        public static bool ended = false;

        public static int currentCameraCueIndex;
        public static Vector3 startPointPos;
        public static Vector3 startPointRot;

        public static float timer;
        public static float percent;

        //Function to get the time a tick lasts in miliseconds
        public static float GetTickTime(float bpm)
        {
            return 60000 / (bpm * 480);
        }

        //Function to verify if camera settings are good
        void CheckCamera()
        {
            //If spectator cam is on
            bool camOn = PlayerPreferences.I.SpectatorCam.Get();
            if (camOn)
            {
                //If spectator cam is set to static third person
                float camMode = PlayerPreferences.I.SpectatorCamMode.Get();
                if (camMode == 1)
                {
                    //If camOK is already true at this point we don't need to do anything
                    if (!camOK)
                    {
                        //If it's not, get reference for SpectatorCam class and set camOK to true
                        spectatorCam = UnityEngine.Object.FindObjectOfType<SpectatorCam>();
                        camOK = true;
                    }
                }
                else { camOK = false; }
            }
            else { camOK = false; }
        }

        public static void MouseAwake()
        {
            Camera thirdPersonCam = spectatorCam.cam;
            Vector3 euler = thirdPersonCam.gameObject.transform.rotation.eulerAngles;
            xAxis = euler.x;
            yAxis = euler.y;
            isMouseAwake = true;
        }

        public static bool LoadCameraCues()
        {
            string path = dir + selectedSong + ".json";
            if (!File.Exists(path)) { return false; }

            cameraCues = Decoder.GetCameraCues(File.ReadAllText(path));
            SpawnText("Camera cues loaded");

            return true;
        }

        public static void LoadFOV()
        {
            string path = dir + selectedSong + ".json";
            tempFov = Decoder.GetFOV(File.ReadAllText(path));

            fovSetting = PlayerPreferences.I.SpectatorCamFOV.Get();
            SetFOV(tempFov);
        }

        public static void SetFOV(float fov)
        {
            PlayerPreferences.I.SpectatorCamFOV.Set(fov);
            spectatorCam.mFov = fov;
            spectatorCam.UpdateSettings();
            spectatorCam.UpdateFOV();
        }

        public static void ResetState()
        {
            lastTick = 0;
            oldLastTick = 0;
            currentCameraCueIndex = 0;
            ended = false;
        }

        public static void SpawnText(string text)
        {
            KataConfig.I.CreateDebugText(text, debugTextPos, 5f, null, persistent: false, 0.2f);
        }

        public override void OnUpdate()
        {
            //Tracking menu state
            menuState = MenuState.GetState();

            //If menu changes
            if (menuState != oldMenuState)
            {
                MelonLogger.Log("Menu: " + menuState.ToString());

                if (menuState == MenuState.State.MainPage)
                {
                    CheckCamera();
                }

                if (menuState == MenuState.State.LaunchPage)
                {
                    scriptExists = LoadCameraCues();
                }

                if (menuState == MenuState.State.Launched && !KataConfig.I.practiceMode)
                {
                    if (scriptExists) { LoadFOV(); }

                    Camera thirdPersonCam = spectatorCam.cam;
                    startPointPos = thirdPersonCam.gameObject.transform.position;
                    startPointRot = thirdPersonCam.gameObject.transform.rotation.eulerAngles;
                }

                if (oldMenuState == MenuState.State.Launched && menuState == MenuState.State.SongPage)
                {
                    SetFOV(fovSetting);
                }

                oldMenuState = menuState;
            }

            //If playing a song
            if (menuState == MenuState.State.Launched && !KataConfig.I.practiceMode)
            {
                //Update midi tick
                lastTick = ScoreKeeper.I.mLastTick;

                if (lastTick != oldLastTick)
                {
                    if (camOK && scriptExists)
                    {
                        Camera thirdPersonCam = spectatorCam.cam;
                        CameraCue cameraCue = cameraCues[currentCameraCueIndex];

                        if (!ended && lastTick >= cameraCue.tick && timer <= cameraCue.tickLength && lastTick <= cameraCue.tick + cameraCue.tickLength)
                        {
                            timer += lastTick - oldLastTick;
                            percent = timer / cameraCue.tickLength;

                            Vector3 destinationPos = new Vector3(cameraCue.xPos, cameraCue.yPos, cameraCue.zPos);
                            thirdPersonCam.gameObject.transform.position = startPointPos + (destinationPos - startPointPos) * percent;

                            Vector3 destinationRot = new Vector3(cameraCue.xRot, cameraCue.yRot, cameraCue.zRot);
                            thirdPersonCam.gameObject.transform.rotation = Quaternion.Euler(
                                startPointRot.y + (destinationRot.y - startPointRot.y) * percent,
                                startPointRot.x + (destinationRot.x - startPointRot.x) * percent,
                                startPointRot.z + (destinationRot.z - startPointRot.z) * percent
                                );
                        }

                        if (!ended && lastTick >= cameraCue.tick + cameraCue.tickLength)
                        {
                            if (timer != 0)
                            {
                                Vector3 destinationPos = new Vector3(cameraCue.xPos, cameraCue.yPos, cameraCue.zPos);
                                thirdPersonCam.gameObject.transform.position = destinationPos;

                                Vector3 destinationRot = new Vector3(cameraCue.xRot, cameraCue.yRot, cameraCue.zRot);
                                thirdPersonCam.gameObject.transform.rotation = Quaternion.Euler(destinationRot.y, destinationRot.x, destinationRot.z);

                                startPointPos = thirdPersonCam.gameObject.transform.position;
                                startPointRot = thirdPersonCam.gameObject.transform.rotation.eulerAngles;
                                timer = 0;

                                if (cameraCues.Length > currentCameraCueIndex + 1) { currentCameraCueIndex += 1; }
                                else { ended = true; }
                            }
                        }
                    }
                    oldLastTick = lastTick;
                }
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                Camera thirdPersonCam = spectatorCam.cam;
                Vector3 homePos = new Vector3(0.0f, 2.4f, -2.6f);
                thirdPersonCam.gameObject.transform.position = homePos;
            }

            if (Input.GetKeyDown(KeyCode.T))
            {
                Camera thirdPersonCam = spectatorCam.cam;
                Vector3 homePos = new Vector3(0.0f, 2.4f, 22.4f);
                thirdPersonCam.gameObject.transform.position = homePos;
            }

            if (Input.GetKeyDown(KeyCode.Y))
            {
                Camera thirdPersonCam = spectatorCam.cam;
                Vector3 homePos = new Vector3(0.0f, 2.4f, -60.6f);
                thirdPersonCam.gameObject.transform.position = homePos;
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                Camera thirdPersonCam = spectatorCam.cam;

                Vector3 camPos = thirdPersonCam.gameObject.transform.position;
                MelonLogger.Log("Cam Pos: " + camPos.ToString());

                Vector3 euler = thirdPersonCam.gameObject.transform.rotation.eulerAngles;
                MelonLogger.Log("Cam Rot: " + euler.ToString());
            }

        }
    }
    public class CameraCue
    {
        public int tick;
        public int tickLength;
        public float xPos;
        public float yPos;
        public float zPos;
        public float xRot;
        public float yRot;
        public float zRot;
    }
    public class Decoder
    {
        public static CameraCue[] GetCameraCues(string data)
        {
            var cameraCuesJSON = JSON.Parse(data);

            CameraCue[] cameraCues = new CameraCue[cameraCuesJSON["cameraCues"].Count];

            for (int i = 0; i < cameraCuesJSON["cameraCues"].Count; i++)
            {
                CameraCue cameraCue = new CameraCue
                {
                    tick = cameraCuesJSON["cameraCues"][i]["tick"],
                    tickLength = cameraCuesJSON["cameraCues"][i]["tickLength"],
                    xPos = cameraCuesJSON["cameraCues"][i]["xPos"],
                    yPos = cameraCuesJSON["cameraCues"][i]["yPos"],
                    zPos = cameraCuesJSON["cameraCues"][i]["zPos"],
                    xRot = cameraCuesJSON["cameraCues"][i]["xRot"],
                    yRot = cameraCuesJSON["cameraCues"][i]["yRot"],
                    zRot = cameraCuesJSON["cameraCues"][i]["zRot"]
                };

                cameraCues[i] = cameraCue;
            }

            return cameraCues;
        }

        public static float GetFOV(string data)
        {
            var cameraCuesJSON = JSON.Parse(data);

            return cameraCuesJSON["fov"];
        }
    }

    internal static class Hooks
    {
        [HarmonyPatch(typeof(SongSelectItem), "OnSelect")]
        private static class SongSelectItemOnSelectPatch
        {
            private static void Postfix(SongSelectItem __instance)
            {
                string songID = __instance.mSongData.songID;

                AudicaMod.selectedSong = songID;
                AudicaMod.ResetState();
            }
        }

        [HarmonyPatch(typeof(InGameUI), "ReturnToSongList")]
        private static class ReturnToSongListPatch
        {
            private static void Postfix(InGameUI __instance)
            {
                if (!KataConfig.I.practiceMode)
                {
                    AudicaMod.SetFOV(AudicaMod.fovSetting);
                }
            }
        }

        [HarmonyPatch(typeof(InGameUI), "Restart")]
        private static class RestartPatch
        {
            private static void Postfix(InGameUI __instance)
            {
                if (!KataConfig.I.practiceMode)
                {
                    AudicaMod.ResetState();
                }
            }
        }

        [HarmonyPatch(typeof(SpectatorCam), "Update")]
        private static class SpectatorCamUpdatePatch
        {
            private static bool Prefix(SpectatorCam __instance)
            {
                if (AudicaMod.camOK)
                {
                    return false;
                }
                else
                {
                    if (AudicaMod.isMouseAwake)
                    {
                        AudicaMod.isMouseAwake = false;
                    }
                    return true;
                }
            }
            private static void Postfix(SpectatorCam __instance)
            {
                if (AudicaMod.camOK)
                {
                    Camera thirdPersonCam = AudicaMod.spectatorCam.cam;

                    if (!AudicaMod.isMouseAwake) { AudicaMod.MouseAwake(); }

                    if (Input.GetKey(KeyCode.W))
                    {
                        thirdPersonCam.gameObject.transform.position = thirdPersonCam.gameObject.transform.position + thirdPersonCam.gameObject.transform.forward;
                    }

                    if (Input.GetKey(KeyCode.S))
                    {
                        thirdPersonCam.gameObject.transform.position = thirdPersonCam.gameObject.transform.position - thirdPersonCam.gameObject.transform.forward;
                    }

                    if (Input.GetKey(KeyCode.A))
                    {
                        thirdPersonCam.gameObject.transform.position = thirdPersonCam.gameObject.transform.position - thirdPersonCam.gameObject.transform.right;
                    }

                    if (Input.GetKey(KeyCode.D))
                    {
                        thirdPersonCam.gameObject.transform.position = thirdPersonCam.gameObject.transform.position + thirdPersonCam.gameObject.transform.right;
                    }

                    if (Input.GetKey(KeyCode.Space))
                    {
                        thirdPersonCam.gameObject.transform.position = thirdPersonCam.gameObject.transform.position + thirdPersonCam.gameObject.transform.up;
                    }

                    if (Input.GetKey(KeyCode.C))
                    {
                        thirdPersonCam.gameObject.transform.position = thirdPersonCam.gameObject.transform.position - thirdPersonCam.gameObject.transform.up;
                    }

                    if (Input.GetKey(KeyCode.Mouse1))
                    {
                        float MIN_X = 0.0f;
                        float MAX_X = 360.0f;
                        float MIN_Y = -90.0f;
                        float MAX_Y = 90.0f;

                        AudicaMod.xAxis += Input.GetAxis("Mouse X") * (AudicaMod.mouseSensitivity * Time.deltaTime);
                        /*
                        if (xAxis < MIN_X) xAxis = MIN_X;
                        else if (xAxis > MAX_X) xAxis = MAX_X;
                        */

                        AudicaMod.yAxis -= Input.GetAxis("Mouse Y") * (AudicaMod.mouseSensitivity * Time.deltaTime);
                        /*
                        if (yAxis < MIN_Y) yAxis = MIN_Y;
                        else if (yAxis > MAX_Y) yAxis = MAX_Y;
                        */

                        thirdPersonCam.gameObject.transform.rotation = Quaternion.Euler(AudicaMod.yAxis, AudicaMod.xAxis, 0.0f);
                    }

                    if (Input.GetKey(KeyCode.Z))
                    {
                        Vector3 euler = thirdPersonCam.gameObject.transform.rotation.eulerAngles;
                        thirdPersonCam.gameObject.transform.rotation = Quaternion.Euler(euler.y, euler.x, euler.z + 1.0f);
                    }

                    if (Input.GetKey(KeyCode.X))
                    {
                        Vector3 euler = thirdPersonCam.gameObject.transform.rotation.eulerAngles;
                        thirdPersonCam.gameObject.transform.rotation = Quaternion.Euler(euler.y, euler.x, euler.z - 1.0f);
                    }

                }
            }
        }
    }
}



