//Copyright 2017 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

//parts taken from Game Window Mover script, original notes for that here:

//Source from http://answers.unity3d.com/questions/179775/game-window-size-from-editor-window-in-editor-mode.html
//Modified by seieibob for use at the Virtual Environment and Multimodal Interaction Lab at the University of Maine.
//Use however you'd like!

using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using HoloPlay;

using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;

namespace HoloPlaySDK_UI
{
    // ? add handles
    // https://docs.unity3d.com/ScriptReference/Handles.html
    [InitializeOnLoad]
    [CustomEditor(typeof(Capture))]
    public class CaptureEditor : Editor
    {
        SerializedProperty size;
        SerializedProperty nearClipFactor;
        SerializedProperty farClipFactor;
        SerializedProperty orthographic;
        SerializedProperty fov;
        SerializedProperty advancedFoldout;
        Capture capture;
        SerializedObject serializedCam;

        void OnEnable()
        {
            capture = (Capture)target;
            serializedCam = new SerializedObject(capture.cam);
            size = serializedObject.FindProperty("size");
            nearClipFactor = serializedObject.FindProperty("nearClipFactor");
            farClipFactor = serializedObject.FindProperty("farClipFactor");
            orthographic = serializedCam.FindProperty("orthographic");
            fov = serializedObject.FindProperty("fov");
            advancedFoldout = serializedObject.FindProperty("advancedFoldout");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            serializedCam.Update();

            GUI.color = Misc.guiColorLight;
            EditorGUILayout.LabelField(Misc.GetFullName(), EditorStyles.centeredGreyMiniLabel);
            GUI.color = Color.white;

            GUI.color = Misc.guiColor;
            EditorGUILayout.LabelField("- Camera -", EditorStyles.whiteMiniLabel);
            GUI.color = Color.white;

            EditorGUILayout.PropertyField(size);
            EditorGUILayout.PropertyField(nearClipFactor);
            EditorGUILayout.PropertyField(farClipFactor);

            advancedFoldout.boolValue = EditorGUILayout.Foldout(advancedFoldout.boolValue, "Advanced", true);
            if (advancedFoldout.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(orthographic);
                if (EditorGUI.EndChangeCheck())
                {
                    Debug.LogWarning(Misc.warningText + "Disable and re-enable for change to take effect");
                }

                GUI.enabled = !orthographic.boolValue;
                EditorGUILayout.PropertyField(fov);
                GUI.enabled = true;

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            serializedObject.ApplyModifiedProperties();
            serializedCam.ApplyModifiedProperties();
        }
    }
}