using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

using System.Runtime.InteropServices;


//Enabling this component allows the HPInput class to give you touches and camera streams available from the camera directly. Only one application can use the camera at the same time, including Unity.

//the 'static' version of the depth plugin, where the camera is accessed directly through the dll.
//for this not to jitter and hang, it requires a multi-threaded solution, provided internally.

namespace HoloPlay
{

    //[ExecuteInEditMode]
    public class depthPluginStatic : depthPluginBase
    {


        [Tooltip("Higher value means more responsive but more noise, lower value means smoother touches but more latency.")]
        [Range(0f, 1f)]
        public float pluginLerp = .85f;

        /// <summary>
        /// Configure smoothness vs responsiveness.  Higher percent means more responsive, lower will be smoother, more interpolated input.
        /// </summary>
        /// <param name="percent"></param>
        [DllImport(pluginName)]
        public static extern void setTouchLerp(float percent);

        [DllImport(pluginName)]
        public static extern void logInternalVariables();

        [DllImport(pluginName)]
        private static extern System.IntPtr getCameraInfo();
        public static Utils.CameraDescription GetPhysicalCameraInfo()
        {
            if (!hasDepthCamera())
                return null;

            System.IntPtr strPtr = getCameraInfo();
            if (strPtr == System.IntPtr.Zero)
                return null;
            string jsonString = Marshal.PtrToStringAnsi(strPtr);

            Utils.CameraDescription d = new Utils.CameraDescription();
            JsonUtility.FromJsonOverwrite(jsonString, d);

            return d;
        }


        /// <summary>
        /// loads the projection map in the default file.
        /// </summary>
        /// <param name="mapNum">specify which map in the file you want to use</param>
        /// <returns></returns>
        [DllImport(pluginName)]
        public static extern bool loadProjectionMap(int mapNum);

        [DllImport(pluginName)]
        public static extern void outputCalibratedTouches(bool trueFalse); //can be used to let the calibrator output raw pixel coords
        [DllImport(pluginName)]
        public static extern void setCalibration(string calibrationFileContents); //used by calibration tool, not intended for app use (the plugin finds and reads the calibration file, itself)
        [DllImport(pluginName)]
        public static extern bool setProjectionMapping(string calibrationFileContents, int mapNum); //used by calibration tool, not intended for app use (the plugin finds and reads the calibration file, itself)

        private Thread thread;
        private depthCamThread pluginThread;

#if HOLOPLAY_NO_CLIENT
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
        }
#endif

        void OnEnable()
		{
#if !HOLOPLAY_NO_CLIENT
            Debug.LogError("Can't use depthPluginStatic without HOLOPLAY_NO_CLIENT defined!  Use the depthPluginClient component instead.");
            Destroy(this.gameObject);
            return;
#else
            pluginThread = new depthCamThread();
			setTouchLerp(pluginLerp);

			thread = new Thread(new ThreadStart(pluginThread.runForever));
			thread.Start();
#endif
        }

#if HOLOPLAY_NO_CLIENT
        private void OnValidate()
        {
            setTouchLerp(pluginLerp);
        }


        protected override void Update() //rename this to getTouches?
        {
            if (pluginThread == null || !pluginThread.prepareTouches() || touches == null) //do this first.  if it gives false it means the plugin isn't even loaded.
                return;

            int arrayLength = pluginThread.getTouches(ref touchPool);
         
            ProcessTouches(arrayLength);
            processTextureStreams();
        }



        protected override void Cleanup()
        {
            if (pluginThread != null)
            {
                pluginThread.requestStop();
                pluginThread = null;
            }

            //This reference shouldn't be null at this point anyway.
            if (thread != null)
            {
                thread.Join(); //this will cause a slight hiccup in the main calling thread, but without it can occasionally crash...
                thread = null;
            }
            //thread = null; //1 crash noted with this simpler method


        }
#endif


    }

}
