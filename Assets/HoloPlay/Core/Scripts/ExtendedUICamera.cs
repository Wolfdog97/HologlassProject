//Copyright 2017 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HoloPlay
{

    [ExecuteInEditMode]
    public class ExtendedUICamera : MonoBehaviour
    {
        // todo: make this flip appropriately with a shader if it's on the holoplay sreen

        public UnityEvent onDisplaySetup;

        public static bool secondScreen { get; private set; }

        Camera cam;

        void OnEnable()
        {
            cam = GetComponent<Camera>();

            StartCoroutine(WaitToSetupDisplay());
        }

        IEnumerator WaitToSetupDisplay()
        {
            while (Quilt.Instance == null)
                yield return null;

            SetupSecondDisplay();
        }

        void SetupSecondDisplay()
        {
#if UNITY_STANDALONE_WIN
            if (Display.displays.Length > 1)
            {
                Display.displays[1].Activate();

                // set the quilt cam to the proper one
                int qd = 0;
                foreach (var d in Display.displays)
                {
                    if (d.systemWidth == Quilt.Instance.config.screenW &&
                        d.systemHeight == Quilt.Instance.config.screenH)
                    {
                        break;
                    }
                    qd++;
                }
                Quilt.Instance.quiltCam.targetDisplay = qd;

                // set the UI to the other
                for (int i = 0; i < Display.displays.Length; i++)
                {
                    if (i != qd)
                    {
                        cam.targetDisplay = i;
                        break;
                    }
                }
                cam.clearFlags = CameraClearFlags.SolidColor;
                Debug.Log(Misc.warningText + "Using multiple displays for separate UI");
                secondScreen = true;
            }
            else
#endif
            {
                cam.clearFlags = CameraClearFlags.Nothing;
                if (!Application.isEditor) // don't want to spam editor console with this
                    Debug.Log(Misc.warningText + "Cannot extend UI");
                secondScreen = false;
            }


            if (onDisplaySetup.GetPersistentEventCount() > 0)
                onDisplaySetup.Invoke();
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ExtendedUICamera))]
    public class HoloPlayUICameraEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField(
                // "How HoloPlay UI works:\n\n" +
                // "Some HoloPlay-based products mirror or rotate the screen. " +
                // "Using this prefab as your canvas ensures that the UI will be flipped properly on all systems.",
                "Windows only: if using two displays, use this to put the UI on the main 2D display. " +
                "This is useful if your UI is too dense for display in the Looking Glass.\n\n" +
                "Use the static bool 'ExtendedUICamera.secondScreen' to check if a second screen was successfully setup.\n\n" +
                "'onDisplaySetup' will be invoked once this script is done attempting to setup the second screen.",
                EditorStyles.helpBox
            );

            EditorGUILayout.PropertyField(serializedObject.FindProperty("onDisplaySetup"));

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}