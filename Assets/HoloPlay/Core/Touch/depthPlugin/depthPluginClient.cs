using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Diagnostics;
using System.Runtime.InteropServices;



//Enabling this component allows the HPInput class to give you touches and camera streams available from a server app. 
//The server app is launched automatically.

//The client version of the plugin.  This will receive data from a server in the dll, and passes that to us here in Unity.

namespace HoloPlay
{
    public class depthPluginClient : depthPluginBase
    {
        public bool hideServerConsole = true;

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
		readonly static string serverExePath = Config.configDirName + "/" + "osx/HoloPlayerServer";
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        readonly static string serverExePath = Config.configDirName + "/" + "win/HoloPlayerServer.exe";
#endif

        [DllImport(pluginName)]
        static extern bool prepareTouchCollection();//call this first, returns false if the plugin is not properly loaded
        [DllImport(pluginName)]
        protected static extern float getTouchData(); //then collect a float array piecemeal. 

        [DllImport(pluginName)]
        protected static extern void init();
        [DllImport(pluginName)]
        protected static extern int update();//try to call this at least 60fps
        [DllImport(pluginName)]
        protected static extern void shutDown();//destroy the realsense class inside the plugin


#if I_LIKE_TO_LIVE_DANGEROUSLY //allows connecting to servers across computers.  Not really supported, but defining I_LIKE_TO_LIVE_DANGEROUSLY allows it.  Keep in mind that this may or may not spontaneously fail to work if processors or OS differ.
        [DllImport(pluginName)]
        protected static extern void initClientWithCustomServer(string serverIP, uint serverPort);

        [Tooltip("Not recommended, but you can use these to connect to a server on a different computer.  Keep in mind that differences in type of processor or other difference in hardware can cause this to simply not work.")]
        public string serverIP = "localhost";
        public uint serverPort = 27016;
#endif


        //do you want to give a chance to the depth plugin before it shuts itself down if the server is not found or other problem occurs?
        int errorToleranceMax = 2; 
        int errorCount = 0;


#if !HOLOPLAY_NO_CLIENT
        void Awake()
        {

            if (instance != null && instance != this)
            {
                Destroy(this.gameObject);
                return;
            }

            registerDebugCallback(new debugCallback(debugMethod)); //allow plugin to print to console
            printDebug(debug);

            instance = this;

            launchServer();

        }
#endif

        private void OnEnable()
        {

#if HOLOPLAY_NO_CLIENT
            UnityEngine.Debug.LogError("Can't use depthPluginClient if HOLOPLAY_NO_CLIENT is defined! Use the depthPluginStatic component instead.");
            Destroy(this.gameObject);
#else
            errorCount = 0;
    #if I_LIKE_TO_LIVE_DANGEROUSLY
            initClientWithCustomServer(serverIP, serverPort);
    #else
            init();
    #endif
#endif
        }

        protected override void Update()
        {
            int ret = update();
            if (ret < 0)
            {
                errorCount++;
                if (errorCount >= errorToleranceMax) //shut ourselves down.
                    enabled = false;
                return;
            }
            else
            {
                int arrayLength = getTouches(ref touchPool);
                ProcessTouches(arrayLength);
                processTextureStreams();
            }
        }

        protected override void Cleanup()
        {
            shutDown();
        }


        public bool launchServer()
        {
            //Try to start the server.  Server will destroy itself if it can't connect to a camera.
            //This will happen automatically if there is already a server, or if the camera is unavailable.
            string fullPathToServer;
            Config.GetConfigPathToFile(serverExePath, out fullPathToServer);

            if (!System.IO.File.Exists(fullPathToServer))
            {
                UnityEngine.Debug.LogWarning("Could not find HoloPlay Server executable at " + fullPathToServer);
                return false;
            }

            Process myProcess = new Process();
            try
            {
                myProcess.StartInfo.UseShellExecute = true;
                // You can start any process, HelloWorld is a do-nothing example.
                myProcess.StartInfo.FileName = fullPathToServer;
                myProcess.StartInfo.CreateNoWindow = hideServerConsole;
                myProcess.Start();
                // This code assumes the process you are starting will terminate itself. 
                // Given that is is started without a window so you cannot terminate it 
                // on the desktop, it must terminate itself or you can do it programmatically
                // from this application using the Kill method.
            }
            catch
            {
                UnityEngine.Debug.LogWarning("Failed to launch camera server.");
                return false;
            }
            return true;
        }

        //returns number of active touches
        public int getTouches(ref AirTouch[] touchPool)
        {
            if (touchPool == null)
                return 0;

            prepareTouchCollection();

            int i = 0;
            float v = getTouchData();
            Vector3 pos;
            while (v != -9999f) //-9999 Is the signal that the plugin is done sending data.
            {
                pos.x = v;
                pos.y = getTouchData();
                pos.z = getTouchData();

                touchPool[i].SetPosition(pos);
                i++;

                v = getTouchData(); //the next x
            }
            return i;
        }




    } //class
} //namespace
