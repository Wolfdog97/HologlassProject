using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Runtime.InteropServices;

//this is a base class that contains the common functions between the static depthPluginStatic and depthPluginClient.
//use of depthPluginStatic or depthPluginClient is mutually exclusive.  The default is to use depthPluginClient, which relies on a server (a separate app) to stream the touches and camera data in.
//depthPluginStatic, on the other hand, directly accesses the camera and allows for some advanced features, but only allows 1 application to use the camera at one time
//Therefore, depthPluginStatic forces you to shut down Unity if you want to test out your build, since they both can't share the camera at one time.
//Further, the advanced features allowed by depthPluginStatic are mainly only useful for use in applications like the touch calibrator app.
//depthPluginStatic, can be used by defining HOLOPLAY_NO_CLIENT

namespace HoloPlay
{
    public abstract class depthPluginBase : MonoBehaviour
    {
        //we can choose here whether to interface with the client/server architecture, or directly deal with the camera.
#if HOLOPLAY_NO_CLIENT
        public const string pluginName = "depthPlugin";
#else
        public const string pluginName = "depthPluginClient";
#endif

        [Tooltip("The depth data is 16 bits deep. This comes in from the plugin as 8 bits in the r, and 8 bits in the g. This toggle can be used to make the depth display as a 8bit grayscale image.")]
        public bool convertDepthTexture = false;
        [Tooltip("When converting the depth texture into 8bit depth, significant information is lost. This threshold can be used to bias the values into a desirable gamut.")]
        public float depthTextureThreshold = 4500f; //for displaying 8bit depth only

        public bool debug;
        [DllImport(pluginName)]
        protected static extern void printDebug(bool offOn);

        //debug messages
        protected delegate void debugCallback(string message);
        [DllImport(pluginName)]
        protected static extern void registerDebugCallback(debugCallback callback);
        protected static void debugMethod(string message)
        {
            Debug.Log(Misc.warningText + "Depth Plugin: " + message);
        }

        /// <summary>
        /// Can be used to know if a depth camera is connected and initialized.
        /// </summary>
        /// <returns>True if the realsense was detected and connected to. False if it was not found or failed to connect.</returns>
        [DllImport(pluginName)]
        public static extern bool hasDepthCamera();


        /// These may crash or cause undefined behavior if the texture provided is not the proper format.
        /// called automatically when getDepthTexture() is called.
        [DllImport(pluginName)]
        protected static extern bool setDepthTexture(System.IntPtr tex);
        [DllImport(pluginName)]
        protected static extern bool updateDepthTexture();
        [DllImport(pluginName)]
        protected static extern bool setColorTexture(System.IntPtr tex);
        [DllImport(pluginName)]
        protected static extern bool updateColorTexture();
        [DllImport(pluginName)]
        protected static extern bool setProcessedTexture(System.IntPtr tex);
        [DllImport(pluginName)]
        protected static extern bool updateProcessedTexture();

        /// <summary>
        /// get the x resolution of the depth camera
        /// </summary>
        [DllImport(pluginName)]
        public static extern int getDimX();
        /// <summary>
        /// get the y resolution of the depth camera
        /// </summary>
        [DllImport(pluginName)]
        public static extern int getDimY();
        /// <summary>
        /// get the x resolution of the color camera
        /// </summary>
        [DllImport(pluginName)]
        public static extern int getColorDimX();
        /// <summary>
        /// get the y resolution of the color camera
        /// </summary>
        [DllImport(pluginName)]
        public static extern int getColorDimY();


        //singleton
        protected static depthPluginBase instance;

#if HOLOPLAY_NO_CLIENT
        public static depthPluginStatic Get() { return (depthPluginStatic)instance; }
#else
        public static depthPluginClient Get() { return (depthPluginClient)instance; }
#endif


        void Start()
        {
            int size = getTouchPoolSize();
            touches = new List<AirTouch>();
            touchPool = new AirTouch[size];
            for (int i = 0; i < size; i++)
            {
                touchPool[i] = new AirTouch(i);
            }

#if !CALIBRATOR
            if (!depthPluginStatic.loadProjectionMap(0))//The projection map file can contain various mappings internally. The default map created by LKG for Unity is #0
                Debug.LogWarning("Plugin failed to load projection map."); 
#endif          
        }

#if UNITY_EDITOR
        public void editorUpdate()
        {
            Update();
        }
#endif

        protected void OnDisable()
        {
            stopColorStream();
            stopDepthStream();
            stopProcessedStream();
            registerDebugCallback(null);
            Cleanup();
        }
        protected virtual void Cleanup() { }  //additional cleanup can be done if needed in the sub-class

        protected virtual void Update() 
        {


     
        }


#region ///// touch related

        [DllImport(pluginName)]
        public static extern int getTouchPoolSize();

        [DllImport(pluginName)]
        static extern void applyCalibrationToCoord(float x, float y, float z);
        [DllImport(pluginName)]
        static extern float getCalibratedX();
        [DllImport(pluginName)]
        static extern float getCalibratedY();
        [DllImport(pluginName)]
        static extern float getCalibratedZ();

        /// <summary>
        /// converts a depth texture pixel (pixelX, pixelY, depthValue) into a 3D coordinate
        /// use the methods in LKG.utils to convert the rgb of a pixel in the depth texture into a depth value
        /// </summary>
        /// <param name="v">x,y,z representing the 2D pixel coordinate and raw depth value. Will be changed into the corresponding 3D coordinate</param>
        public static void applyCalibrationToCoordinate(ref Vector3 v)
        {
            applyCalibrationToCoord(v.x, v.y, v.z); //it would be nice to be able to send these as a reference, but it is a problem via managed/unmanaged code
            v.x = getCalibratedX();
            v.y = getCalibratedY();
            v.z = getCalibratedZ();
        }


        
        public static List<AirTouch> touches { get; private set; }
        protected AirTouch[] touchPool;
        protected static readonly Vector3 invalidTouch = new Vector3(-1f, -1f, -1f);



        public Vector3 averageWorld
        {
            get
            {
                if (Capture.Instance)
                    return Capture.Instance.transform.TransformPoint(averageNormalized);
                else
                    return Vector3.zero;
            }
        } /*Average position of active touches, in world position, given the main HoloPlay Capture*/
        public Vector3 averageNormalized { get; private set; } /*Average position of active touches, normalized to the HoloPlay Capture*/
        public Vector3 averageDiff { get; private set; } /*Average difference of active touches, normalized to the HoloPlay Capture*/


        protected void ProcessTouches(int arrayLength)
        {
            if (touches == null) //do this first.  if it gives false it means the plugin isn't even loaded.
                return;

            touches.Clear();

            //handle average positions
            averageNormalized = Vector3.zero;
            averageDiff = Vector3.zero;

            for (int i = 0; i < arrayLength; i++)
            {
                if (touchPool[i].GetLocalPos() != invalidTouch)
                {
                    touches.Add(touchPool[i]);

                    averageNormalized += touchPool[i].GetLocalPos();
                    averageDiff += touchPool[i].GetLocalDiff();
                }
            }

            if (touches.Count == 0)
            {
                averageNormalized = Vector3.zero; //is it better if this is Vector3.zero?  or invalid touch? ..using Vector3.zero so it don't mess up meta scripts that may be tallying
                averageDiff = Vector3.zero;
            }

            else if (touches.Count == 1)
            {
                averageNormalized = touches[0].GetLocalPos();
                averageDiff = touches[0].GetLocalDiff();
            }
            else
            {
                averageNormalized /= (float)touches.Count;
                averageDiff /= (float)touches.Count;

                //TODO get rid of these magic numbers
                if (averageDiff.x < -.3f || averageDiff.x > .3f || averageDiff.y < -.3f || averageDiff.y > .3) //this is too fast to be a real movement, its probably an artifact.
                {
                    averageDiff = Vector3.zero;
                }
            }

            processDepthTouchTargets(touches);        
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////depth touch target code

        //these keep track of all depthTouchTargets, and hence the in input system can send them user input data as it is received.
        protected static HashSet<depthTouchTarget> eventTargets = new HashSet<depthTouchTarget>();
        public static void _setDepthEventTarget(depthTouchTarget t, bool addRemove)
        {
            if (addRemove)
                eventTargets.Add(t);
            else
                eventTargets.Remove(t);
        }
        //this handles the in-editor touches so we can use the same system to move things around in the editor
        protected static HashSet<editorDepthTouchTarget> editorEventTargets = new HashSet<editorDepthTouchTarget>();
        public static void _setEditorDepthEventTarget(editorDepthTouchTarget t, bool addRemove)
        {
            if (addRemove)
                editorEventTargets.Add(t);
            else
                editorEventTargets.Remove(t);
        }

        protected static bool hadTouches = false;
        static void processDepthTouchTargets(List<AirTouch> touches)
        {
#if UNITY_EDITOR
            if (eventTargets.Count == 0 && editorEventTargets.Count == 0)
                return;
#else
            if (eventTargets.Count == 0)
                return;
#endif

            if (touches.Count == 0)
            {

                if (hadTouches)
                {
                    foreach (depthTouchTarget target in eventTargets)
                        target.onNoDepthTouches();
#if UNITY_EDITOR
                    foreach (editorDepthTouchTarget target in editorEventTargets)
                        target.onNoDepthTouches();
#endif
                }

                hadTouches = false;
            }
            else
            {
                foreach (depthTouchTarget target in eventTargets)
                    target.onDepthTouch(touches);
#if UNITY_EDITOR
                foreach (editorDepthTouchTarget target in editorEventTargets)
                    target.onDepthTouch(touches);
#endif

                hadTouches = true;
            }
        }

        ////////////////end depth touch target 
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////
#endregion  //end touch related code


#region ///// camera texture related
        ///////////////////////////////////////
        /////////////////////////////////////// texture streaming code
        //https://forum.unity3d.com/threads/updating-a-texture-live-from-a-c-plugin.100333/

        protected Texture2D depthTexture = null;
        protected Color32[] depthPixels;
        protected GCHandle depthPixelsHandle;
        public Texture2D getDepthTexture()
        {
            if (depthTexture)
                return depthTexture;

            if (!hasDepthCamera() || getDimX() < 2 || getDimY() < 2)
            {
                Debug.LogWarning("Failed to get texture.  Check the script execution order and make sure the HoloPlay SDK depthPlugin component is enabled before getting a texture from it.");
                return null; //The plugin is probably not initialized or something is wrong with the resolutions in the calibration settings.
            }

            depthTexture = new Texture2D(getDimX(), getDimY(), TextureFormat.RGBA32, false);
            depthTexture.filterMode = FilterMode.Point;
            depthPixels = depthTexture.GetPixels32(0);
            depthPixelsHandle = GCHandle.Alloc(depthPixels, GCHandleType.Pinned);

            if (!setDepthTexture(depthPixelsHandle.AddrOfPinnedObject()))
                return null;

            return depthTexture;
        }
        /// <summary>
        /// Once a depth texture has been requested, it will be updated indefinitely.
        /// Call this to reduce the performance hit if you are done using the depth texture.
        /// </summary>
        public void stopDepthStream()
        {
            if (depthPixelsHandle.IsAllocated)
                depthPixelsHandle.Free();

            depthPixels = null;
            depthTexture = null;
            setDepthTexture((System.IntPtr)0); //a null pointer
        }

        protected Texture2D colorTexture = null;
        protected Color32[] colorPixels;
        protected GCHandle colorPixelsHandle;
        public Texture2D getColorTexture()
        {
            if (colorTexture)
                return colorTexture;

            if (!hasDepthCamera() || getColorDimX() < 2 || getColorDimY() < 2)
            {
                Debug.LogWarning("Failed to get texture.  Check the script execution order and make sure the LKG depthPlugin component is enabled before getting a texture from it.");
                return null; //The plugin is probably not initialized or something is wrong with the resolutions in the calibration settings.
            }

            colorTexture = new Texture2D(getColorDimX(), getColorDimY(), TextureFormat.RGBA32, false);
            colorPixels = colorTexture.GetPixels32(0);
            colorPixelsHandle = GCHandle.Alloc(colorPixels, GCHandleType.Pinned);

            if (!setColorTexture(colorPixelsHandle.AddrOfPinnedObject()))
                return null;
            return colorTexture;
        }
        /// <summary>
        /// Once a color texture has been requested, it will be updated indefinitely.
        /// Call this to reduce the performance hit if you are done using the color texture.
        /// </summary>
        public void stopColorStream()
        {
            if (colorPixelsHandle.IsAllocated)
                colorPixelsHandle.Free();

            colorPixels = null;
            colorTexture = null;
            setDepthTexture((System.IntPtr)0); //a null pointer
        }


        protected Texture2D processedTexture = null;
        protected Color32[] processedPixels;
        protected GCHandle processedPixelsHandle;
        public Texture2D getProcessedTexture()
        {
            if (processedTexture)
                return processedTexture;

            if (!hasDepthCamera() || getDimX() < 2 || getDimY() < 2)
            {
                Debug.LogWarning("Failed to get texture.  Check that 1) texture streaming is enabled in the touch.json c onfig file. 2)Check that script execution order and make sure the depthPlugin component is enabled before getting a texture from it.");
                return null; //The plugin is probably not initialized or something is wrong with the resolutions in the calibration settings.
            }

            processedTexture = new Texture2D(getDimX(), getDimY(), TextureFormat.RGBA32, false);
            processedPixels = processedTexture.GetPixels32(0);
            processedPixelsHandle = GCHandle.Alloc(processedPixels, GCHandleType.Pinned);

            if (!setProcessedTexture(processedPixelsHandle.AddrOfPinnedObject()))
                return null;
            return processedTexture;
        }
        /// <summary>
        /// Once a processed texture has been requested, it will be updated indefinitely.
        /// Call this to reduce the performance hit if you are done using the depth texture.
        /// </summary>
        public void stopProcessedStream()
        {
            if (processedPixelsHandle.IsAllocated)
                processedPixelsHandle.Free();

            processedPixels = null;
            processedTexture = null;
            setProcessedTexture((System.IntPtr)0); //a null pointer
        }



        protected void processTextureStreams()
        {
            if (depthTexture)
            {
                updateDepthTexture();
                if (convertDepthTexture)
                {
                    //converts the 16 bit depth data into a visual 8 bit grayscale texture
                    byte b;
                    for (int i = 0; i < depthPixels.Length; i++)
                    {
                        b = Utils.ColorToDepth8(depthPixels[i], depthTextureThreshold);
                        depthPixels[i].r = b;
                        depthPixels[i].g = b;
                        depthPixels[i].b = b;
                    }
                }

                depthTexture.SetPixels32(depthPixels, 0);
                depthTexture.Apply(false, false);
            }
            if (colorTexture)
            {
                updateColorTexture();
                colorTexture.SetPixels32(colorPixels, 0);
                colorTexture.Apply(false, false);
            }
            if (processedTexture)
            {
                updateProcessedTexture();
                processedTexture.SetPixels32(processedPixels, 0);
                processedTexture.Apply(false, false);
            }
        }

        ///////////////////////////////////////
        ///////////////////////////////////////end texture streaming code
#endregion
    }

}