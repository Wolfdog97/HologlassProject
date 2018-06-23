using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

using Object = UnityEngine.Object;

namespace HoloPlay
{

    public class Utils
    {
        public static float AngleBetweenPoints(Vector2 v1, Vector2 v2)
        {
            return Mathf.Atan2(v1.x - v2.x, v1.y - v2.y) * Mathf.Rad2Deg;
        }

        public static Vector3 ScalePointRelativeToTransform(float scaleMod, Vector3 point, Transform t)
        {
            point = t.InverseTransformPoint(point);
            point *= scaleMod;
            return t.TransformPoint(point);
        }

        /// <summary>
        /// Gets a depth value from the raw depth texture
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static System.UInt16 ColorToDepth16(Color32 c)
        {
            return (System.UInt16)((c.g << 8) | c.r);
        }


        /// <summary>
        /// Converts a color value coming in from the depth plugin, and compresses it into an 8 bit depth value
        /// </summary>
        /// <param name="c">the corresponding color from the incoming depth map</param>
        /// <param name="thresholdDistance">the farthest distance that you care to represent in the 8 bits, smaller will give you more precision.  A too low value will cause the value to loop over, causing banding.</param>
        /// <returns></returns>
        public static byte ColorToDepth8(Color32 c, float thresholdDistance)
        {

            System.UInt16 v = ColorToDepth16(c);

            if (v == 0)
                return 0;

            int p = (int)(((float)v / thresholdDistance) * 127f);

            //  if (p > 35) //this is a noise threshold. Sometimes the low values are not accurate.
            return (byte)(255 - p); //the first int here is a noise threshold.
                                    //p = 255 - p;
                                    //  return 0;
        }

        public static string ColorToHex(Color32 color)
        {
            string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
            return hex;
        }
        public static Color HexToColor(string hex)
        {
            hex = hex.Replace("0x", "");//in case the string is formatted 0xFFFFFF
            hex = hex.Replace("#", "");//in case the string is formatted #FFFFFF
            byte a = 255;//assume fully visible unless specified in hex
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            //Only use alpha if the string has enough characters
            if (hex.Length == 8)
            {
                a = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            }
            return new Color32(r, g, b, a);
        }

        /// <summary>
        /// Converts Unity's Vector3 string conversion back to a Vector3
        /// </summary>
        /// <param name="sVector"></param>
        /// <returns></returns>
        public static Vector3 StringToVector3(string sVector)
        {
            // Remove the parentheses
            if (sVector.StartsWith("(") && sVector.EndsWith(")"))
            {
                sVector = sVector.Substring(1, sVector.Length - 2);
            }

            // split the items
            string[] sArray = sVector.Split(',');

            // store as a Vector3
            Vector3 result = new Vector3(
                float.Parse(sArray[0]),
                float.Parse(sArray[1]),
                float.Parse(sArray[2]));

            return result;
        }

        /// <summary>
        /// Converts Unity's Quaternion string conversion back to a Quaternion
        /// </summary>
        /// <param name="sVector"></param>
        /// <returns></returns>
        public static Quaternion StringToQuaternion(string sQuaternion)
        {
            // Remove the parentheses
            if (sQuaternion.StartsWith("(") && sQuaternion.EndsWith(")"))
            {
                sQuaternion = sQuaternion.Substring(1, sQuaternion.Length - 2);
            }

            // split the items
            string[] sArray = sQuaternion.Split(',');

            // store as a Vector3
            Quaternion result = new Quaternion(
                float.Parse(sArray[0]),
                float.Parse(sArray[1]),
                float.Parse(sArray[2]),
                float.Parse(sArray[3])
                );

            return result;
        }


        [System.Serializable]
        public class CameraExtrinsicsMatrix
        {
            public float rXX;
            public float rYX;
            public float rZX;
            public float m03;
            public float rXY;
            public float rYY;
            public float rZY;
            public float m13;
            public float rXZ;
            public float rYZ;
            public float rZZ;
            public float m23;
            public float tX;
            public float tY;
            public float tZ;
            public float m33;

            public Matrix4x4 AsMatrix()
            {
                Matrix4x4 m = new Matrix4x4();
                m.SetColumn(0, new Vector4(rXX, rYX, rZX, m03));
                m.SetColumn(1, new Vector4(rXY, rYY, rZY, m13));
                m.SetColumn(2, new Vector4(rXZ, rYZ, rZZ, m23));
                m.SetColumn(3, new Vector4(tX, tY, tZ, m33));
                return m;
            }
        }

        [System.Serializable]
        public class CameraIntrinsics
        {
            public float focalLengthX;
            public float focalLengthY;
            public float centerOffsetX;
            public float centerOffsetY;
        }

        [System.Serializable]
        public class CameraDescription
        {
            public CameraExtrinsicsMatrix colorToDepthExtrinsics4x4;
            public CameraIntrinsics colorIntrinsics;
            public CameraExtrinsicsMatrix depthToColorExtrinsics4x4;
            public CameraIntrinsics depthIntrinsics;
        }
    }

}
