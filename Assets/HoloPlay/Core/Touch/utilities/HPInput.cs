using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using HoloPlay;

public static class HPInput
{

    #region WAND (WandInterface)
    public static class Wand
    {
        //TODO

    }
    #endregion


    #region CAMERA STREAMS
    public static class CameraStreams
    {

        /// <summary>
        /// The depth stream is 16 bits deep. This is represented as 8bit in r and 8bit in g.
        /// If you want a grayscale image, set this to true, and it will be post processed for you into a 'normal' texture, but losing fidelity.
        /// </summary>
        /// <param name="trueFalse"></param>
        public static void ConvertDepthStream(bool trueFalse)
        {
            if (depthPluginBase.Get())
                depthPluginBase.Get().convertDepthTexture = trueFalse;
        }

        /// <summary>
        /// Note that by default color texture streaming is off in the server/client version.
        /// If you want the color stream, you can do 1 of several things:
        /// A) Enable the stream in the LKG_calibration/touch.json (note this may cause instability if the computer is not fast enough, or if on OSX priority switching may cause apps including Unity itself to hang. Use the 'nice' command to force a high priority to apps.)
        /// B) You can switch to using the 'static' version of the plugin.  This causes only 1 app to use the camera at a time (including unity), but full resolution of depth and color become available in a stable way.
        /// </summary>
        /// <returns>A texture that contains the color texture stream from the camera.</returns>
        public static Texture2D GetColor()
        {
            if (depthPluginBase.Get())
            {
                Texture2D t = depthPluginBase.Get().getColorTexture();
#if !HOLOPLAY_NO_CLIENT
                if (!t)
                    Debug.LogWarning("Color texture appears to be unavailable. Make sure your touch.json config have it enabled, or switch to 'static' LKG plugin mode by using the HOLOPLAY_NO_CLIENT preprocessor.");
#endif
                return t;
            }
            return null;
        }

        public static void StopColorStream()
        {
            if (depthPluginBase.Get())
            {
                depthPluginBase.Get().stopColorStream();
            }
        }

        /// <summary>
        /// Note that by default depth texture streams in at a low resolution in the server/client version.
        /// If you want a higher res depth stream, you can do 1 of several things:
        /// A) remove or set to 0 the downsampling in the LKG_calibration/touch.json (note this may cause instability if the computer is not fast enough, or if on OSX priority switching may cause apps including Unity itself to hang. Use the 'nice' command to force a high priority to apps.)
        /// B) You can switch to using the 'static' version of the plugin.  This causes only 1 app to use the camera at a time (including unity), but full resolution of all streams become available in a stable way.
        /// </summary>
        /// <returns>A texture that contains the depth texture stream from the camera.</returns>
        public static Texture2D GetDepth()
        {
            if (depthPluginBase.Get())
            {
                Texture2D t = depthPluginBase.Get().getDepthTexture();
#if !HOLOPLAY_NO_CLIENT
                if (!t)
                    Debug.LogWarning("Depth texture appears to be unavailable. Make sure your touch.json config have it enabled, or switch to 'static' LKG plugin mode by using the HOLOPLAY_NO_CLIENT preprocessor.");
#endif
                return t;
            }
            return null;
        }

        public static void StopDepthStream()
        {
            if (depthPluginBase.Get())
            {
                depthPluginBase.Get().stopDepthStream();
            }
        }

        /// <summary>
        /// Note that by default processed texture streaming is off in the server/client version.
        /// If you want the processed stream, you can do 1 of several things:
        /// A) Enable the stream in the LKG_calibration/touch.json (note this may cause instability if the computer is not fast enough, or if on OSX priority switching may cause apps including Unity itself to hang. Use the 'nice' command to force a high priority to apps.)
        /// B) You can switch to using the 'static' version of the plugin.  This causes only 1 app to use the camera at a time (including unity), but full resolution of all streams become available in a stable way.
        /// </summary>
        /// <returns>A texture that contains the processed texture stream from the camera.</returns>
        public static Texture2D GetProcessed()
        {
            if (depthPluginBase.Get())
            {
                Texture2D t = depthPluginBase.Get().getProcessedTexture();
                #if !HOLOPLAY_NO_CLIENT
                if (!t)
                    Debug.LogWarning("Processed texture appears to be unavailable. Make sure your touch.json config have it enabled, or switch to 'static' LKG plugin mode by using the HOLOPLAY_NO_CLIENT preprocessor.");
                #endif
                    return t;
            }
            return null;
        }

        public static void StopProcessedStream()
        {
            if (depthPluginBase.Get())
            {
                depthPluginBase.Get().stopProcessedStream();
            }
        }
    }
#endregion


#region DEPTH/AIR TOUCHES
    public static class AirTouches
    {
        static public List<AirTouch> Touches
        {
            get
            {
                if (depthPluginClient.Get())
                {
                    return depthPluginBase.touches;
                }
                return null;
            }
            
        }


        static Vector3 AverageWorldPos
        {
            get
            {
                if (depthPluginClient.Get())
                {
                    return depthPluginBase.Get().averageWorld;
                }
                return Vector3.zero;
            }
        }

        static Vector3 AverageNormalizedPos
        {
            get
            {
                if (depthPluginClient.Get())
                {
                    return depthPluginBase.Get().averageNormalized;
                }
                return Vector3.zero;
            }
        }

        static Vector3 AverageDiffNormalized
        {
            get
            {
                if (depthPluginClient.Get())
                {
                    return depthPluginBase.Get().averageDiff;
                }
                return Vector3.zero;
            }
        }

    }
#endregion

#region TOUCH SCREEN INPUT (TouchScreenInterface)
    public static class TouchScreen
    {
        //touchscreen not yet supported
    }
#endregion


}


