
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Runtime.InteropServices;


//this thread class is meant to be used when the non-client (direct camera access) plugin is used.
//that way, the hardware is kept from lagging Unity or the executable.

namespace HoloPlay
{

    public class depthCamThread
    {
       // public const string pluginName = "depthPlugin"; //this must be hardcoded to the static plugin, or it will cause crashes if used on the client version.

        [DllImport(depthPluginStatic.pluginName)]
        public static extern bool hasDepthCamera(); //note that calling this will instantiate the plugin


        [DllImport(depthPluginStatic.pluginName)]
        static extern void init();
        [DllImport(depthPluginStatic.pluginName)]
        static extern int update();//try to call this at least 60fps
        [DllImport(depthPluginStatic.pluginName)]
        static extern void shutDown();//destroy the realsense class inside the plugin

        [DllImport(depthPluginStatic.pluginName)]
        static extern bool prepareTouchCollection();//call this first, returns false if the plugin is not properly loaded
        [DllImport(depthPluginStatic.pluginName)]
        public static extern float getTouchData(); //then collect a float array piecemeal. 



        public depthCamThread()
        {
            init();         
        }


        ///////debug message handler
        private delegate void debugCallback(string message);
        [DllImport(depthPluginStatic.pluginName)]
        private static extern void registerDebugCallback(debugCallback callback);

        private static void debugMethod(string message)
        {
   //         if (message.Contains("error"))
   //             shutDown();

            Debug.Log("HoloPlaySDK Depth: " + message);
        }
        ///////debug message handler


        // ------------------------------------------------------------------------
        // Invoked to indicate to this thread object that it should stop.
        // ------------------------------------------------------------------------
        private bool stopRequested = false;
        public void requestStop()
        {
            lock (this)
            {
                stopRequested = true;
            }
        }


        /// <summary>
        /// stop the thread.
        /// </summary>
        protected void stop()
        {
            stopRequested = true;
        }



        public void runForever()
        {
            // This try is for having a log message in case of an unexpected
            // exception.
            try
            {

                while (!IsStopRequested())
                {
                    try
                    {
                        runOnce();
                    }
                    catch (System.Exception ioe)
                    {
                        Debug.LogWarning("Exception: " + ioe.Message + "\nStackTrace: " + ioe.StackTrace);
                    }
                }

                // Attempt to do a final cleanup. 
                shutDown();
            }
            catch (System.Exception e)
            {
                Debug.LogError("Unknown exception: " + e.Message + " " + e.StackTrace);
                stopRequested = true;
                shutDown();
            }
        }


        // ------------------------------------------------------------------------
        // Just checks if 'RequestStop()' has already been called in this object.
        // ------------------------------------------------------------------------
        private bool IsStopRequested()
        {
            lock (this)
            {
                return stopRequested;
            }
        }

        // ------------------------------------------------------------------------
        // A single iteration of the semi-infinite loop.
        // ------------------------------------------------------------------------
        private void runOnce()
        {
            try
            {
                int ret = update();
                if (ret < 0)
                {
                    stop();
                    Debug.Log("LKG static plugin thread encountered a bad return: " + ret + ".  Stopping thread.");
                }
                return;
            }
            catch (System.TimeoutException)
            {
                // This is normal, not every time we have a report from the serial device
                return;
            }

        }

        public bool prepareTouches()
        {
            lock (this)
            {
                return prepareTouchCollection();
            }
        }


        //returns number of active touches
        public int getTouches(ref AirTouch[] touchPool)
        {
            if (touchPool == null)
                return 0;

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

    }
}