//Copyright 2017 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace HoloPlay
{
    [ExecuteInEditMode]
    public class Capture : MonoBehaviour
    {
        /// <summary>
        /// Static ref to the most recently active Capture.
        /// </summary>
        public static Capture Instance { get; private set; }

        [Range(0.01f, 500)]
        [Tooltip("Size of the Capture. Use this, rather than the transform's scale, to resize the capture.")]
        [SerializeField]
        float size = 5;
        /// <summary>
        /// The size of the Capture. 
        /// Use this, rather than the transform's scale, to resize the Capture.
        /// </summary>
        public float Size
        {
            get { return size; }
            set
            {
                size = value;
                SetupCam();
            }
        }

        [Range(8, 90)]
        [Tooltip("FOV is determined by calibration. Changing it WILL cause a discrepancy between touch input and visual input.")]
        [SerializeField]
        float fov = 13.5f;
        public float Fov
        {
            get { return fov; }
            set
            {
                fov = value;
                SetupCam();
            }
        }

        // ? maybe actually implement this
        // public bool sizeFollowsTransform;

        [Range(0f, 6f)]
        [Tooltip("Larger value = more distance *in front* the focal plane is rendered.\n" +
            "Objects too far in front or behind the focal plane will appear blurry and double-image.")]
        [FormerlySerializedAs("nearClip")]
        public float nearClipFactor = 0.5f;

        [Range(0.01f, 6f)]
        [Tooltip("Larger value = more distance *behind* the focal plane is rendered.\n" +
            "Objects too far in front or behind the focal plane will appear blurry and double-image.")]
        [FormerlySerializedAs("farClip")]
        public float farClipFactor = 0.5f;

        /// <summary>
        /// The Camera doing the rendering of the views.
        /// This camera moves around the focal pane, taking x number of renders 
        /// (where x is the number of views)
        /// </summary>
        /// <returns></returns>
        public Camera cam;

        private RenderTexture tempRT;

        // hold on to vertical angle and aspect for setting up cam w/o help from quilt
        private float verticalAngle = 0f;
        private float aspect = 1f;

        /// <summary>
        /// On View Render callback.
        /// It passes an int which is the 0th indexed view being rendered (so from 0 to numViews-1).
        /// It passes a second int which is the total number of views (numViews).
        /// This event fires once every time a view is rendered, just before the render happens.
        /// It fires one last time after the last render, passing the int numViews.
        /// </summary>
        public static Action<int, int> onViewRender;

#if UNITY_EDITOR
        // for the editor script
        [SerializeField] bool advancedFoldout;
#endif

        void OnEnable()
        {
            // set up the static ref
            Instance = this;
            CreateCam();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (!cam) return;
            SetupCam();
        }
#endif

        public void RenderView(float horizontalAngle, float verticalAngle)
        {
            HandleOffset(horizontalAngle, verticalAngle);
            cam.fieldOfView = 60f; // fixes the shadows
            cam.Render();
            cam.fieldOfView = fov;
        }

        // returns the cam distance after adjustment for FOV.
        public float GetAdjustedDistance()
        {
            if (cam.orthographic)
                return 0;

            return size / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
        }


        public void HandleOffset(float horizontalAngle, float verticalAngle)
        {
            // start from scratch
            cam.ResetWorldToCameraMatrix();
            cam.ResetProjectionMatrix();

            float adjustedSize = GetAdjustedDistance();

            // orthographic or regular perspective
            if (cam.orthographic)
            {
                cam.transform.localPosition = Vector3.zero;
                cam.transform.localEulerAngles = Vector3.up * -horizontalAngle;
                return;
            }

            //* perspective correction
            //* imagine triangle from pivot center, to camera, to camera's ideal new position. 
            //* offAngle is angle at the pivot center. solve for offsetX
            //* tan(offAngle) = offX / camDist
            //* offX = camDist * tan(offAngle)
            float offsetX = adjustedSize * Mathf.Tan(horizontalAngle * Mathf.Deg2Rad);
            float offsetY = adjustedSize * Mathf.Tan(verticalAngle * Mathf.Deg2Rad);

            // view matrix
            var viewMatrix = cam.worldToCameraMatrix;
            viewMatrix.m03 -= offsetX;
            viewMatrix.m13 -= offsetY;
            cam.worldToCameraMatrix = viewMatrix;

            // proj matrix
            var projMatrix = cam.projectionMatrix;
            projMatrix.m02 -= offsetX / (size * cam.aspect);
            projMatrix.m12 -= offsetY / size;
            cam.projectionMatrix = projMatrix;
        }

        public void CreateCam()
        {
            string camName = "HoloPlay Camera";
            Transform camChild = transform.Find(camName);

            // don't destroy it if it exists already, only create it if it doesn't
            if (camChild == null)
            {
                camChild = new GameObject(camName, typeof(Camera)).transform;
                camChild.parent = transform;
                cam = camChild.GetComponent<Camera>();
                cam.clearFlags = CameraClearFlags.Color;
                cam.backgroundColor = Color.black;
            }
            else
            {
                cam = camChild.GetComponent<Camera>();
            }
        }

        public void SetupCam()
        {
            SetupCam(this.aspect, this.verticalAngle);
        }

        public void SetupCam(float aspect, float verticalAngle, bool resetOffset = true)
        {
            // set some fields for future uses
            this.aspect = aspect;
            this.verticalAngle = verticalAngle;

            if (!cam) return;
            float adjustedDistance = GetAdjustedDistance();
            cam.transform.localRotation = Quaternion.identity;
            cam.transform.localPosition = new Vector3(0, 0, -adjustedDistance);
            cam.aspect = aspect;
            cam.nearClipPlane = adjustedDistance - nearClipFactor * size;
            if (!cam.orthographic)
                cam.nearClipPlane = Mathf.Max(0.001f, cam.nearClipPlane);
            cam.farClipPlane = adjustedDistance + farClipFactor * size;
            cam.fieldOfView = fov;
            cam.orthographicSize = size;

            if (resetOffset)
                HandleOffset(0, verticalAngle);
        }

#if UNITY_EDITOR
        // ? add color selection back to this
        public void DrawCaptureGizmos(int j)
        {
            Gizmos.color = Misc.gizmoColor[j];
            //get corners
            List<Vector3> fc = new List<Vector3>();
            fc.AddRange(Misc.GetFrustumCorners(cam, cam.nearClipPlane));
            fc.AddRange(Misc.GetFrustumCorners(cam, cam.farClipPlane));

            Misc.DrawVolume(fc);

            //focal point
            Gizmos.color = Misc.gizmoColor0[j];
            var foc = Misc.GetFrustumCorners(cam, GetAdjustedDistance());
            for (int i = 0; i < foc.Length; i++)
            {
                var i0 = i != 0 ? i - 1 : foc.Length - 1;
                var i1 = i != foc.Length - 1 ? i + 1 : 0;

                var f = foc[i];
                var f0 = Vector3.Lerp(foc[i], foc[i0], 0.1f);
                var f1 = Vector3.Lerp(foc[i], foc[i1], 0.1f);

                Gizmos.DrawLine(f, f0);
                Gizmos.DrawLine(f, f1);
            }

            //arrow
            if (UnityEditor.SceneView.lastActiveSceneView.camera != null)
            {
                var forward = transform.forward * size * 2f;
                var aRelPos = -forward * nearClipFactor * 0.5f;
                var aPos = aRelPos + transform.position - forward * 0.15f;
                var editorCamPos = UnityEditor.SceneView.lastActiveSceneView.camera.transform.position;
                var cross = Vector3.Cross(editorCamPos - transform.position, aRelPos - forward * 0.06f);
                cross = cross.normalized * size * 0.12f;

                Gizmos.DrawLine(aPos, aPos - forward * 0.24f);
                Gizmos.DrawLine(aPos, aPos - forward * 0.06f + cross);
                Gizmos.DrawLine(aPos, aPos - forward * 0.06f - cross);
            }

            // logo
            Gizmos.color = Misc.gizmoLogoColor[j];
            var gl = new List<Vector3>();
            var s = Vector3.Distance(fc[0], fc[1]);

            foreach (var g in Misc.gizmoLogo)
            {
                gl.Add(transform.rotation * ((g + new Vector2(1, 1)) * s * 0.02f) + fc[0]);
            }

            foreach (var g in Misc.gizmoLogo)
            {
                gl.Add(transform.rotation * ((g + new Vector2(1, 2)) * s * 0.02f) + fc[0]);
            }

            for (int i = 0; i < 4; i++)
            {
                var i0 = i != 3 ? i + 1 : 0;
                Gizmos.DrawLine(gl[i], gl[i0]);
                Gizmos.DrawLine(gl[i + 4], gl[i0 + 4]);
            }

            Gizmos.DrawLine(gl[0], gl[4]);
            Gizmos.DrawLine(gl[2], gl[6]);
        }
#endif
    }
}