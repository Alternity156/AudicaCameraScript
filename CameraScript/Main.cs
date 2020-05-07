using System;
using System.IO;
using MelonLoader;
using NET_SDK;
using NET_SDK.Harmony;
using UnityEngine;
using NET_SDK.Reflection;
using UnityEngine.UI;
using Il2CppSystem.Xml.Serialization;

namespace CameraScript
{
    public static class BuildInfo
    {
        public const string Name = "CameraScript"; // Name of the Mod.  (MUST BE SET)
        public const string Author = "Alternity"; // Author of the Mod.  (Set as null if none)
        public const string Company = null; // Company that made the Mod.  (Set as null if none)
        public const string Version = "0.1.0"; // Version of the Mod.  (MUST BE SET)
        public const string DownloadLink = null; // Download Link for the Mod.  (Set as null if none)
    }

    public class CameraScript : MelonMod
    {
        public static Patch SpectatorCam_Update;
        public static Patch SongSelectItem_OnSelect;

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

        public static CameraCue[] cameraCues;
        public static int fov;

        public static float lastTick;
        public static float oldLastTick;

        public static int currentCameraCueIndex;
        public static Vector3 startPoint;

        public static float timer;
        public static float percent;

        //Function to get the time a tick lasts in miliseconds
        public static float  GetTickTime(float bpm)
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
                } else { camOK = false; }
            } else { camOK = false; }
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

            return true;
        }

        public static void LoadFOV()
        {
            string path = dir + selectedSong + ".json";
            fov = Decoder.GetFOV(File.ReadAllText(path));
        }

        public override void OnApplicationStart()
        {
            if (!Directory.Exists(dir)) { Directory.CreateDirectory(Application.dataPath + "/../Mods/Config/CameraScript"); }

            Instance instance = Manager.CreateInstance("CameraScript");

            CameraScript.SpectatorCam_Update = instance.Patch(SDK.GetClass("SpectatorCam").GetMethod("Update"), typeof(CameraScript).GetMethod("SpectatorCamUpdate"));
            CameraScript.SongSelectItem_OnSelect = instance.Patch(SDK.GetClass("SongSelectItem").GetMethod("OnSelect"), typeof(CameraScript).GetMethod("OnSelect"));
        }

        public static unsafe void SpectatorCamUpdate(IntPtr @this)
        {
            if (camOK)
            {
                Camera thirdPersonCam = spectatorCam.cam;

                if (!isMouseAwake) { MouseAwake(); }

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

                    xAxis += Input.GetAxis("Mouse X") * (mouseSensitivity * Time.deltaTime);
                    /*
                    if (xAxis < MIN_X) xAxis = MIN_X;
                    else if (xAxis > MAX_X) xAxis = MAX_X;
                    */

                    yAxis -= Input.GetAxis("Mouse Y") * (mouseSensitivity * Time.deltaTime);
                    /*
                    if (yAxis < MIN_Y) yAxis = MIN_Y;
                    else if (yAxis > MAX_Y) yAxis = MAX_Y;
                    */

                    thirdPersonCam.gameObject.transform.rotation = Quaternion.Euler(yAxis, xAxis, 0.0f);
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
            else
            {
                CameraScript.SpectatorCam_Update.InvokeOriginal(@this);
                if (isMouseAwake)
                {
                    isMouseAwake = false;
                }
            }
        }

        //Tracking selected song
        public static void OnSelect(IntPtr @this)
        {
            CameraScript.SongSelectItem_OnSelect.InvokeOriginal(@this);

            SongSelectItem button = new SongSelectItem(@this);
            string songID = button.mSongData.songID;

            selectedSong = songID;
        }

        public override void OnUpdate()
        {
            //Tracking menu state
            menuState = MenuState.GetState();

            //If menu changes
            if (menuState != oldMenuState)
            {
                oldMenuState = menuState;
                MelonModLogger.Log("Menu: " + menuState.ToString());

                //Check if camera state has changed
                CheckCamera();

                if (menuState == MenuState.State.Launched)
                {
                    lastTick = 0;
                    oldLastTick = 0;
                    currentCameraCueIndex = 0;
                    scriptExists = LoadCameraCues();
                    if (scriptExists) { LoadFOV(); }

                    Camera thirdPersonCam = spectatorCam.cam;
                    startPoint = thirdPersonCam.gameObject.transform.position;
                }
            }

            //If playing a song
            if (menuState == MenuState.State.Launched)
            {
                //Update midi tick
                lastTick = ScoreKeeper.I.mLastTick;
                if (lastTick < 0) { lastTick = 0; }
                if (lastTick != oldLastTick)
                {
                    CameraCue cameraCue = cameraCues[currentCameraCueIndex];

                    if (lastTick >= cameraCue.tick && timer <= cameraCue.tickLength && lastTick <= cameraCue.tick + cameraCue.tickLength)
                    {
                        Camera thirdPersonCam = spectatorCam.cam;

                        timer += lastTick-oldLastTick;
                        percent = timer / cameraCue.tickLength;

                        Vector3 destinationPos = new Vector3(cameraCue.xPos, cameraCue.yPos, cameraCue.zPos);
                        Vector3 destinationRot = new Vector3(cameraCue.xRot, cameraCue.yRot, cameraCue.zRot);

                        thirdPersonCam.gameObject.transform.position = startPoint + (destinationPos - startPoint) * percent;

                        if (timer >= cameraCue.tickLength)
                        {
                            startPoint = thirdPersonCam.gameObject.transform.position;
                            timer = 0;

                            if (cameraCues.Length > currentCameraCueIndex)
                            {
                                currentCameraCueIndex += 1;
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
                MelonModLogger.Log("Cam Pos: " + camPos.ToString());

                Vector3 euler = thirdPersonCam.gameObject.transform.rotation.eulerAngles;
                MelonModLogger.Log("Cam Rot: " + euler.ToString());
            }
            
        }

        /*
        public override void OnApplicationQuit()
        {
            MelonModLogger.Log("OnApplicationQuit");
        }

        public override void OnLevelWasLoaded(int level)
        {
            MelonModLogger.Log("OnLevelWasLoaded: " + level.ToString());
        }

        public override void OnLevelWasInitialized(int level)
        {
            MelonModLogger.Log("OnLevelWasInitialized: " + level.ToString());
        }

        public override void OnFixedUpdate()
        {
            MelonModLogger.Log("OnFixedUpdate");
        }

        public override void OnLateUpdate()
        {
            MelonModLogger.Log("OnLateUpdate");
        }

        public override void OnGUI()
        {
            MelonModLogger.Log("OnGUI");
        }

        public override void OnModSettingsApplied()
        {
            MelonModLogger.Log("OnModSettingsApplied");
        }
        */
    }
}
