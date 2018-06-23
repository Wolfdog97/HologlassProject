//Copyright 2017 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HoloPlay
{
    public static class Misc
    {
        /// <summary>
        /// Release version of the SDK. 
        /// </summary>
        public static readonly float version = 0.64f;

        // if it's not a beta version, set this to 0
        public static readonly int beta = 6;

        public static string GetVersionName()
        {
            return version.ToString("#0.00#") + (beta == 0 ? "" : " beta " + beta);
        }

        public static string GetFullName()
        {
            return "HoloPlay " + GetVersionName();
        }

        //* debug */
        public readonly static string warningText = "[HoloPlay] ";

        //* screen forcing */
        public static string[] comArgs;

        public static void ReadCommandLineArgs()
        {
            comArgs = System.Environment.GetCommandLineArgs();
        }

        // public static void CopyTexture(Texture src, RenderTexture dest, int x, int y)
        // {
        //     if (SystemInfo.copyTextureSupport == UnityEngine.Rendering.CopyTextureSupport.None)
        //     {
        //         Graphics.SetRenderTarget(dest);
        //         GL.PushMatrix();
        //         GL.LoadPixelMatrix(0, dest.width, dest.height, 0);
        //         Graphics.DrawTexture(new Rect(x, y, src.width, src.height), src);
        //         GL.PopMatrix();
        //     }
        //     else
        //     {
        //         Graphics.CopyTexture(src, 0, 0, 0, 0, src.width, src.height, dest, 0, 0, x, y);
        //     }
        // }

        public static IEnumerator QuickApplicationReset()
        {
            //for good measure
            ReadCommandLineArgs();
            for (int i = 0; i < 10; i++)
            {
                yield return null;
            }

            System.Diagnostics.Process p = System.Diagnostics.Process.GetCurrentProcess();
            string args = "";

            if (Application.platform != RuntimePlatform.WindowsPlayer)
            {
                args = "--args ";
            }

            foreach (var arg in comArgs)
            {
                args += arg + " ";
            }

            System.Diagnostics.Process.Start(p.MainModule.FileName, args);
            Application.Quit();
        }

        public static bool GetNextFilename(string path, string filename, string extension, out string fullName, out string fullPath)
        {
            if (!Directory.Exists(path))
            {
                Debug.Log(warningText + "Invalid path");
                fullName = filename;
                fullPath = path;
                return false;
            }

            int num = 0;
            do
            {
                fullName = filename + "_" + num.ToString("#000") + extension;
                fullPath = Path.Combine(path, fullName);
                num++;
            }
            while (File.Exists(fullPath));

            return true;
        }

        //* inspector */
#if UNITY_EDITOR
        public readonly static Color guiColor = Color.HSVToRGB(0.35f, 1, UnityEditor.EditorGUIUtility.isProSkin ? 1 : 0.5f);
        public readonly static Color guiColorLight = Color.HSVToRGB(0.35f, 0.3f, 1);

        //* gizmo stuff */
        /// <summary>
        /// Gizmo Color.
        /// For use in editor only, sets the color of the gizmo in scene view.
        /// </summary>
        public readonly static Color[] gizmoColor = new Color[]
        {
            Color.HSVToRGB(0.35f, 1, 1),
            Color.HSVToRGB(0.55f, 1, 1),
            Color.HSVToRGB(0.75f, 1, 1),
            Color.HSVToRGB(0.95f, 1, 1),
            Color.HSVToRGB(0.15f, 1, 1),
        };
        public readonly static Color[] gizmoColor0 = new Color[]
        {
            Color.HSVToRGB(0.5f, 1, 1),
            Color.HSVToRGB(0.7f, 1, 1),
            Color.HSVToRGB(0.9f, 1, 1),
            Color.HSVToRGB(0.1f, 1, 1),
            Color.HSVToRGB(0.3f, 1, 1),
        };
        public readonly static Color[] gizmoLogoColor = new Color[]
        {
            Color.HSVToRGB(0.8f, 0.8f, 1),
            Color.HSVToRGB(0.0f, 0.8f, 1),
            Color.HSVToRGB(0.2f, 0.8f, 1),
            Color.HSVToRGB(0.4f, 0.8f, 1),
            Color.HSVToRGB(0.6f, 0.8f, 1),
        };
        public readonly static bool gizmoShowAll;
        public readonly static Vector2[] gizmoLogo = new[]
        {
            new Vector2(0, 1), new Vector2(2, 2), new Vector2(4, 1), new Vector2(2, 0),
            new Vector2(0, 2), new Vector2(2, 3), new Vector2(4, 2), new Vector2(2, 1),
        };

        /// <summary>
        /// Draws a 6 sided gizmo shape, given the 8 corners (clockwise front to clockwise back)
        /// </summary>
        /// <param name="v"></param>
        public static void DrawVolume(List<Vector3> v)
        {
            if (v.Count != 8) return;

            //draw near square
            Gizmos.DrawLine(v[0], v[1]);
            Gizmos.DrawLine(v[1], v[2]);
            Gizmos.DrawLine(v[2], v[3]);
            Gizmos.DrawLine(v[3], v[0]);

            //draw far square
            Gizmos.DrawLine(v[0 + 4], v[1 + 4]);
            Gizmos.DrawLine(v[1 + 4], v[2 + 4]);
            Gizmos.DrawLine(v[2 + 4], v[3 + 4]);
            Gizmos.DrawLine(v[3 + 4], v[0 + 4]);

            //connect them
            Gizmos.DrawLine(v[0], v[0 + 4]);
            Gizmos.DrawLine(v[1], v[1 + 4]);
            Gizmos.DrawLine(v[2], v[2 + 4]);
            Gizmos.DrawLine(v[3], v[3 + 4]);
        }

        /// <summary>
        /// returns the corners of the camera frustum as vector3s at dist
        /// </summary>
        /// <param name="cam"></param>
        /// <param name="dist"></param>
        /// <param name="relative"></param>
        /// <returns></returns>
        public static Vector3[] GetFrustumCorners(Camera cam, float dist)
        {
            // make sure the dist is actually within the camera's clipping area
            dist = Mathf.Clamp(dist, cam.nearClipPlane, cam.farClipPlane);

            // get corners
            Vector3[] frustumCorners = new[]
            {
                cam.ViewportToWorldPoint(new Vector3(0, 0, dist)),
                cam.ViewportToWorldPoint(new Vector3(0, 1, dist)),
                cam.ViewportToWorldPoint(new Vector3(1, 1, dist)),
                cam.ViewportToWorldPoint(new Vector3(1, 0, dist))
            };

            return frustumCorners;
        }
#endif
    }
}