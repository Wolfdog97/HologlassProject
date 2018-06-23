//Copyright 2017 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using HoloPlay;

namespace HoloPlaySDK_UI
{
    [InitializeOnLoad]
    [CustomEditor(typeof(Quilt))]
    public class QuiltEditor : Editor
    {
        SerializedProperty captures;
        SerializedProperty overrideTexture;
        SerializedProperty quiltRT;
        SerializedProperty tilesX;
        SerializedProperty tilesY;
        SerializedProperty quiltSize;
        SerializedProperty tilingPresetIndex;
        SerializedProperty tileSizeX;
        SerializedProperty tileSizeY;
        SerializedProperty numViews;
        SerializedProperty onQuiltSetup;
        SerializedProperty advancedFoldout;
        SerializedProperty renderIn2D;
        // SerializedProperty editModeIn2D;
        SerializedProperty debugPrintoutKey;
        SerializedProperty screenshotKey;
        SerializedProperty screenshotName;
#if CALIBRATOR
        SerializedProperty config;
#endif
#if MULTI_LKG
        SerializedProperty visualName;
#endif

        void OnEnable()
        {
            captures = serializedObject.FindProperty("captures");
            overrideTexture = serializedObject.FindProperty("overrideTexture");
            quiltRT = serializedObject.FindProperty("quiltRT");
            tilesX = serializedObject.FindProperty("tiling").FindPropertyRelative("tilesX");
            tilesY = serializedObject.FindProperty("tiling").FindPropertyRelative("tilesY");
            quiltSize = serializedObject.FindProperty("tiling").FindPropertyRelative("quiltSize");
            tilingPresetIndex = serializedObject.FindProperty("tilingPresetIndex");
            tileSizeX = serializedObject.FindProperty("tileSizeX");
            tileSizeY = serializedObject.FindProperty("tileSizeY");
            numViews = serializedObject.FindProperty("numViews");
            onQuiltSetup = serializedObject.FindProperty("onQuiltSetup");
            advancedFoldout = serializedObject.FindProperty("advancedFoldout");
            renderIn2D = serializedObject.FindProperty("renderIn2D");
            // editModeIn2D = serializedObject.FindProperty("editModeIn2D");
            debugPrintoutKey = serializedObject.FindProperty("debugPrintoutKey");
            screenshotKey = serializedObject.FindProperty("screenshotKey");
            screenshotName = serializedObject.FindProperty("screenshotName");
#if CALIBRATOR
            config = serializedObject.FindProperty("config");
#endif
#if MULTI_LKG
            visualName = serializedObject.FindProperty("visualName");
#endif
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            Quilt quilt = (Quilt)target;

            EditorGUILayout.Space();

            GUI.color = Misc.guiColor;
            EditorGUILayout.LabelField("- Quilt -", EditorStyles.whiteMiniLabel);
            GUI.color = Color.white;

            GUI.enabled = false;
            EditorGUILayout.PropertyField(quiltRT);
            GUI.enabled = true;

            EditorGUILayout.PropertyField(screenshotKey);

            EditorGUILayout.PropertyField(screenshotName);

            advancedFoldout.boolValue = EditorGUILayout.Foldout(
                advancedFoldout.boolValue,
                "Advanced",
                true
            );
            if (advancedFoldout.boolValue)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(captures, true);
                EditorGUILayout.PropertyField(overrideTexture);

                List<string> tilingPresetNames = new List<string>();
                foreach (var p in Quilt.tilingPresets)
                {
                    tilingPresetNames.Add(p.name);
                }
                tilingPresetNames.Add("Custom");

                EditorGUI.BeginChangeCheck();
                tilingPresetIndex.intValue = EditorGUILayout.Popup(
                    "Tiling",
                    tilingPresetIndex.intValue,
                    tilingPresetNames.ToArray()
                );
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                }

                // if it's a custom
                if (tilingPresetIndex.intValue >= Quilt.tilingPresets.Length)
                {
                    EditorGUILayout.PropertyField(tilesX);
                    EditorGUILayout.PropertyField(tilesY);
                    EditorGUILayout.PropertyField(quiltSize);
                }

                EditorGUI.indentLevel--;

                // tiles information
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.LabelField(
                    numViews.displayName + ": " + numViews.intValue.ToString(),
                    EditorStyles.miniLabel
                );

                EditorGUILayout.LabelField(
                    "Tiles: " + tilesX.intValue + " x " + tilesY.intValue.ToString(),
                    EditorStyles.miniLabel
                );

                EditorGUILayout.LabelField(
                    quiltSize.displayName + ": " +
                        quiltSize.intValue.ToString() + " x " +
                        quiltSize.intValue.ToString() + " px",
                    EditorStyles.miniLabel
                );

                EditorGUILayout.LabelField(
                    "Tile Size: " + tileSizeX.intValue.ToString() + " x " +
                        tileSizeY.intValue.ToString() + " px",
                    EditorStyles.miniLabel
                );

                EditorGUILayout.EndVertical();

                // on quilt setup event
                EditorGUILayout.PropertyField(onQuiltSetup);
            }

            EditorGUILayout.Space();

            GUI.color = Misc.guiColor;
            EditorGUILayout.LabelField("- Preview -", EditorStyles.whiteMiniLabel);
            GUI.color = Color.white;

            EditorGUILayout.PropertyField(renderIn2D);
            // todo: add if people ask
            // GUI.enabled = !renderIn2D.boolValue;
            // EditorGUILayout.PropertyField(editModeIn2D);
            // GUI.enabled = true;

            string previewerShortcutKey = "Ctrl + E";
            string settingsShortcutKey = "Ctrl + Shift + E";
#if UNITY_EDITOR_OSX
            previewerShortcutKey = "⌘E";
            settingsShortcutKey = "⌘^E";
#endif

            if (GUILayout.Button(new GUIContent(
                "Toggle Preview (" + previewerShortcutKey + ")",
                "If your LKG device is set up as a second display, " +
                "this will generate a game window on it to use as a " +
                "realtime preview"),
                EditorStyles.miniButton
            ))
            {
                PreviewWindow.ToggleWindow();
            }

            if (GUILayout.Button(new GUIContent(
                "Settings (" + settingsShortcutKey + ")",
                "Use to set previewer position"),
                EditorStyles.miniButton
            ))
            {
                EditorApplication.ExecuteMenuItem("HoloPlay/Preview Settings");
            }

            EditorGUILayout.Space();

            GUI.color = Misc.guiColor;
            EditorGUILayout.LabelField("- Config -", EditorStyles.whiteMiniLabel);
            GUI.color = Color.white;

#if CALIBRATOR
            EditorGUILayout.PropertyField(config, true);
#endif
#if MULTI_LKG
            EditorGUILayout.PropertyField(visualName);
#endif
            EditorGUILayout.PropertyField(debugPrintoutKey);

            if (GUILayout.Button(new GUIContent(
                "Reload Config",
                "Reload the config, only really necessary if " +
                "you edited externally and the new config settings won't load"),
                EditorStyles.miniButton
            ))
            {
                quilt.LoadConfig();
            }

            EditorGUILayout.Space();

            GUI.color = Misc.guiColor;
            EditorGUILayout.LabelField("- Project Settings -", EditorStyles.whiteMiniLabel);
            GUI.color = Color.white;

            if (GUILayout.Button(new GUIContent(
                "Optimization Settings",
                "Open a window that will let you select project settings " +
                "to be optimized for best performance with HoloPlay"),
                EditorStyles.miniButton
            ))
            {
                OptimizationSettings window = EditorWindow.GetWindow<OptimizationSettings>();
                window.Show();
            }

            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
        }
    }
}