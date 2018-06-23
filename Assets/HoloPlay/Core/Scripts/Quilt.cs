//Copyright 2017 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HoloPlay
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class Quilt : MonoBehaviour
    {
        //**********/
        //* fields */
        //**********/

        /// <summary>
        /// Static ref to the most recently active Quilt.
        /// </summary>
        public static Quilt Instance { get; private set; }

        public Camera quiltCam { get; private set; }

        public static readonly int quiltCamLayer = 29; //hopefully random enough not to cause issues

        /// <summary>
        /// The Captures this quilt will call render from
        /// </summary>
        [Tooltip("The HoloPlay Captures rendering to the QuiltRT. This Quilt calls Render on each of the Captures in this array in order")]
        public Capture[] captures;

        /// <summary>
        /// The material with the lenticular shader. The Quilt sets values for this material based on the calibration
        /// </summary>
        public Material lenticularMat;

        /// <summary>
        /// The actual rendertexture that gets drawn to the screen
        /// </summary>
        [Tooltip("The rendertexture that gets processed through the Lneticular material and spit to the screen")]
        public RenderTexture quiltRT;

        /// <summary>
        /// Useful for loading quilts directly instead of depending on a capture
        /// </summary>
        [Tooltip("Set this texture to load a quilt manually. Make sure to adjust the tiling settings to match.")]
        public Texture overrideTexture;

        private RenderTexture decoyRT;

        private RenderTexture tileRT;

        /// <summary>
        /// Gets called for each view being rendered. Passes first the view number being rendered, then the number of views. 
        /// Gets called once per view render, then a final time after rendering is complete with viewBeingRendered equal to the number of views.
        /// </summary>
        public static Action<int, int> onViewRender;

        public enum QuiltSize
        {
            _1024 = 1024,
            _2048 = 2048,
            _4096 = 4096,
        }

        [Serializable]
        public struct Tiling
        {
            public string name;

            [Range(1, 16)]
            public int tilesX;

            [Range(1, 16)]
            public int tilesY;

            [Range(512, 4096)]
            public int quiltSize;

            public Tiling(string name, int tilesX, int tilesY, int quiltSize)
            {
                this.name = name;
                this.tilesX = tilesX;
                this.tilesY = tilesY;
                this.quiltSize = quiltSize;
            }
        }

        public Tiling tiling = new Tiling("Default", 4, 8, 2048);

        public static readonly Tiling[] tilingPresets = new Tiling[]{
            new Tiling(
                "Standard", 4, 8, 2048
            ),
            new Tiling(
                "High Res", 5, 9, 4096
            ),
            new Tiling(
                "High View", 6, 10, 4096
            ),
            new Tiling(
                "2D", 1, 1, 1024
            ),
        };

        [SerializeField]
        private int tilingPresetIndex;
        public int TilingPresetIndex
        {
            get { return tilingPresetIndex; }
            set
            {
                tilingPresetIndex = value;
                ApplyPreset();
            }
        }

        public int numViews;

        public int tileSizeX;

        public int tileSizeY;

        public int paddingX;

        public int paddingY;

        public float portionX;

        public float portionY;

        public float aspect;

        public Config.VisualConfig config;

        [SerializeField]
        private KeyCode debugPrintoutKey = KeyCode.F9;

        [SerializeField]
        private KeyCode screenshotKey = KeyCode.F10;

        [SerializeField]
        private string screenshotName = "screenshot";

        // todo: document how this works
#if MULTI_LKG
        public enum VisualName
        {
            visual,
            visual2,
            visual3,
            visual4,
        }

        public VisualName visualName;
#endif

        /// <summary>
        /// Happens in OnEnable after config is loaded, screen is setup, material is created, and config is sent to shader
        /// </summary>
        public UnityEvent onQuiltSetup;

        [SerializeField]
        [Tooltip("Emulate the HoloPlayer Buttons using 1/2/3/4/5 on alphanumeric keyboard. Off by default.")]
        bool emulateHPButtons;

#if UNITY_EDITOR
        // for the editor script
        [SerializeField]
        bool advancedFoldout;

        [SerializeField]
        [Tooltip("Render in 2D. If set to true, the application will still render in 3D in play mode and in builds.")]
        bool renderIn2D = false;

        // [SerializeField]
        // [Tooltip("Render in 2D in edit mode only. If set to true, the application will still render in 3D in play mode and in builds. " +
        //     "Useful if you are trying to work without draining battery, etc.")]
        // bool editModeIn2D = false;
#endif

        //***********/
        //* methods */
        //***********/

        void OnEnable()
        {
            Instance = this;

            LoadConfig();

            SetupScreen();

            ApplyPreset();

            foreach (var capture in captures)
            {
                if (!capture) continue;
                capture.SetupCam(aspect, config.verticalAngle);
            }

            if (onQuiltSetup.GetPersistentEventCount() > 0)
                onQuiltSetup.Invoke();

#if MULTI_LKG
            for (int i = 0; i < Display.displays.Length; i++)
            {
                Display.displays[i].Activate();
            }
#endif
        }

        void OnDisable()
        {
            if (quiltRT && quiltRT.IsCreated())
            {
                quiltRT.Release();
                DestroyImmediate(quiltRT);
            }
            DestroyImmediate(lenticularMat);
        }

        void Update()
        {
            if (Input.GetKeyDown(debugPrintoutKey))
            {
                var currentDebugPrintouts = GetComponents<DebugPrintout>();
                if (currentDebugPrintouts.Length > 0)
                {
                    foreach (var c in currentDebugPrintouts)
                    {
                        Destroy(c);
                    }
                }
                else
                {
                    var printout = gameObject.AddComponent<DebugPrintout>();
                    printout.keyName = debugPrintoutKey.ToString();
                }
            }

#if CALIBRATOR || UNITY_EDITOR //! temporary fix
            // if the calibrator is running, ALWAYS be passing config to the material
            PassConfigToMaterial();
#endif

            if (Input.GetKeyDown(screenshotKey))
            {
                StartCoroutine(Screenshot());
            }
        }

        void OnValidate()
        {
            ApplyPreset();

            foreach (var capture in captures)
            {
                if (!capture) continue;
                // i think i disabled this because it happens elsewhere
                // ! but may need to reenable it
                // capture.SetupCam(aspect, config.verticalAngle);
            }
        }

        void OnPreRender()
        {
            quiltCam.targetTexture = decoyRT;
        }

        void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            // clear rt
            Graphics.SetRenderTarget(quiltRT);
            GL.Clear(false, true, Color.black);
            if (overrideTexture)
            {
                GL.PushMatrix();
                GL.LoadOrtho();
                Graphics.DrawTexture(new Rect(0, 1, 1, -1), overrideTexture);
                GL.PopMatrix();
            }

            // render views
            for (int i = 0; i < numViews; i++)
            {
                // broadcast the onViewRender action
                if (onViewRender != null && Application.isPlaying)
                    onViewRender(i, numViews);
                // ? one thing that might be nice is using camera depth as the ordering for this
                foreach (var capture in captures)
                {
                    if (!capture || !capture.isActiveAndEnabled) continue;
                    capture.SetupCam(aspect, config.verticalAngle, false);
                    tileRT = RenderTexture.GetTemporary(tileSizeX, tileSizeY, 24);
                    capture.cam.targetTexture = tileRT;
                    capture.RenderView(AngleAtView(i), config.verticalAngle);
                    CopyToQuiltRT(i, tileRT);
                    capture.cam.targetTexture = null;
                    RenderTexture.ReleaseTemporary(tileRT);
                }
            }

            // reset cameras so they are back to center
            foreach (var capture in captures)
            {
                if (!capture) continue;
                capture.HandleOffset(aspect, config.verticalAngle);
            }

            Graphics.Blit(quiltRT, dest, lenticularMat);

        }

        void OnPostRender()
        {
            quiltCam.targetTexture = null;
        }

        // todo: let the user load config 2 or 3 for second displays
        public void LoadConfig()
        {
            Config.VisualConfig loadedConfig = new Config.VisualConfig();
            int index = 0;
#if MULTI_LKG
            index = (int)visualName;
#endif
            if (!Config.LoadVisualFromFile(out loadedConfig, Config.visualFileNames[index]))
            {
                // todo: print an on-screen warning about the config not being available
            }

            config = loadedConfig;
        }

        public void SetupValues()
        {
            numViews = tiling.tilesX * tiling.tilesY;
            tileSizeX = (int)tiling.quiltSize / tiling.tilesX;
            tileSizeY = (int)tiling.quiltSize / tiling.tilesY;
            paddingX = (int)tiling.quiltSize - tiling.tilesX * tileSizeX;
            paddingY = (int)tiling.quiltSize - tiling.tilesY * tileSizeY;
            portionX = (float)tiling.tilesX * tileSizeX / (float)tiling.quiltSize;
            portionY = (float)tiling.tilesY * tileSizeY / (float)tiling.quiltSize;

            if (config != null)
                aspect = config.screenW / config.screenH;
        }

        public void SetupQuilt()
        {
            quiltCam = GetComponent<Camera>();
            if (quiltCam == null)
                gameObject.AddComponent<Camera>();
            quiltCam.enabled = true;
            quiltCam.useOcclusionCulling = false;
            quiltCam.cullingMask = 1 << quiltCamLayer;
            quiltCam.clearFlags = CameraClearFlags.Nothing;
            quiltCam.orthographic = true;
            quiltCam.orthographicSize = 0.01f;
            quiltCam.nearClipPlane = -0.01f;
            quiltCam.farClipPlane = 0.01f;
            quiltCam.stereoTargetEye = StereoTargetEyeMask.None;

            lenticularMat = new Material(Shader.Find("HoloPlay/Lenticular"));
            if (config != null)
                PassConfigToMaterial();

            decoyRT = new RenderTexture(4, 4, 0);

            quiltRT = new RenderTexture((int)tiling.quiltSize, (int)tiling.quiltSize, 0)
            {
                filterMode = FilterMode.Point,
                autoGenerateMips = false,
                useMipMap = false
            };
            quiltRT.Create();
        }

        public float AngleAtView(int view)
        {
            if (numViews <= 1)
                return 0;

            return -config.viewCone * 0.5f + (float)view / (numViews - 1f) * config.viewCone;
        }

        public void CopyToQuiltRT(int view, RenderTexture rt)
        {
            // copy to fullsize rt
            int ri = numViews - view - 1;
            int x = (view % tiling.tilesX) * tileSizeX;
            int y = (ri / tiling.tilesX) * tileSizeY;
            // the padding is necessary because the shader takes y from the opposite spot as this does
            Rect rtRect = new Rect(x, y + paddingY, tileSizeX, tileSizeY);

            if (rt.IsCreated() && quiltRT.IsCreated())
            {
                Graphics.SetRenderTarget(quiltRT);
                GL.PushMatrix();
                GL.LoadPixelMatrix(0, (int)tiling.quiltSize, (int)tiling.quiltSize, 0);
                Graphics.DrawTexture(rtRect, rt);
                GL.PopMatrix();
            }
        }

        //* sending variables to the shader */
        public void PassConfigToMaterial()
        {
            float screenInches = (float)config.screenW / config.DPI;
            float newPitch = config.pitch * screenInches;
            newPitch *= Mathf.Cos(Mathf.Atan(1f / config.slope));
            lenticularMat.SetFloat("pitch", newPitch);

            float newTilt = config.screenH / (config.screenW * config.slope);
            newTilt *= config.flipImageX.asBool ? -1 : 1;
            lenticularMat.SetFloat("tilt", newTilt);

            lenticularMat.SetFloat("center", config.center);
            lenticularMat.SetFloat("invView", config.invView);
            lenticularMat.SetFloat("flipX", config.flipImageX);
            lenticularMat.SetFloat("flipY", config.flipImageY);

            float subp = 1f / (config.screenW * 3f);
            subp *= config.flipImageX.asBool ? -1 : 1;
            lenticularMat.SetFloat("subp", subp);

            lenticularMat.SetInt("ri", !config.flipSubp.asBool ? 0 : 2);
            lenticularMat.SetInt("bi", !config.flipSubp.asBool ? 2 : 0);

            lenticularMat.SetVector("tile", new Vector4(
                tiling.tilesX,
                tiling.tilesY,
                portionX,
                portionY
            ));
        }

        public void ApplyPreset()
        {
            if (tilingPresetIndex < tilingPresets.Length)
            {
                tiling = tilingPresets[tilingPresetIndex];
            }

#if UNITY_EDITOR
            // if (renderIn2D || (editModeIn2D && !Application.isPlaying))
            if (renderIn2D)
            {
                tiling = new Tiling("2D in editor", 1, 1, 1024);
            }
#endif

            SetupValues();

            SetupQuilt();
        }

        void SetupScreen()
        {
#if UNITY_EDITOR
            if (UnityEditor.PlayerSettings.defaultScreenWidth != config.screenW.asInt ||
                UnityEditor.PlayerSettings.defaultScreenHeight != config.screenH.asInt)
            {
                UnityEditor.PlayerSettings.defaultScreenWidth = config.screenW.asInt;
                UnityEditor.PlayerSettings.defaultScreenHeight = config.screenH.asInt;
            }
#endif

            // if the config is already set, return out
            if (Screen.width == config.screenW.asInt &&
                Screen.height == config.screenH.asInt)
            {
                return;
            }

            Screen.SetResolution(config.screenW.asInt, config.screenH.asInt, true);
        }

        public static string SerializeTilingSettings(Tiling tiling)
        {
            return
                "tx" + tiling.tilesX.ToString("00") +
                "ty" + tiling.tilesY.ToString("00") +
                "ts" + tiling.quiltSize.ToString("0000");
        }

        public static Tiling DeserializeTilingSettings(string str)
        {
            int xi = str.IndexOf("tx");
            int yi = str.IndexOf("ty");
            int si = str.IndexOf("ts");

            if (xi < 0 || yi < 0 || si < 0)
            {
                Debug.Log(Misc.warningText + "Couldn't deserialize tiling settings -- using default");
                return tilingPresets[0];
            }
            else
            {
                string xs = str.Substring(xi + 2, 2);
                string ys = str.Substring(yi + 2, 2);
                string ss = str.Substring(si + 2, 4);

                Tiling tiling = new Tiling(
                    "deserialized",
                    int.Parse(xs),
                    int.Parse(ys),
                    int.Parse(ss)
                );

                return tiling;
            }
        }

        IEnumerator Screenshot()
        {
            var previousTiling = tiling;
            tiling = tilingPresets[0];
            SetupValues();
            SetupQuilt();

            yield return null;

            Texture2D quiltTex = new Texture2D(quiltRT.width, quiltRT.height, TextureFormat.RGB24, false);
            RenderTexture.active = quiltRT;
            quiltTex.ReadPixels(new Rect(0, 0, quiltRT.width, quiltRT.height), 0, 0);
            RenderTexture.active = null;
            var bytes = quiltTex.EncodeToPNG();
            string fullPath;
            string fullName;
            if (!Misc.GetNextFilename(Path.GetFullPath("."), screenshotName + "_" + SerializeTilingSettings(tiling), ".png", out fullName, out fullPath))
            {
                Debug.LogWarning(Misc.warningText + "Couldn't save screenshot");
            }
            else
            {
                // fullFileName += DateTime.Now.ToString(" yyyy MMdd HHmmss");
                // fullFileName = fullFileName.Replace(" ", "_") + ".png";
                File.WriteAllBytes(fullPath, bytes);
                Debug.Log(Misc.warningText + "Wrote screenshot to " + fullName);
            }

            tiling = previousTiling;
            SetupValues();
            SetupQuilt();
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            int i = 0;
            foreach (var capture in captures)
            {
                if (!capture) continue;
                // todo: make sure this doesn't screw up if config isn't loaded yet
                capture.HandleOffset(0, config.verticalAngle);
                capture.DrawCaptureGizmos(i++);
                i = i % Misc.gizmoColor.Length;
            }
        }
#endif
    }
}