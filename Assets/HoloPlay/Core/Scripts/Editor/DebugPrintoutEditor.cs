using UnityEngine;
using UnityEditor;

namespace HoloPlay
{
    [CustomEditor(typeof(DebugPrintout))]
    public class DebugPrintoutEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            GUI.color = Misc.guiColor;
            EditorGUILayout.LabelField("- Debug -", EditorStyles.whiteMiniLabel);
            GUI.color = Color.white;

            EditorGUILayout.HelpBox(
                "Press F9 while in-game to enable debug printout.\n" +
                "Used to display a printout of the SDK version and calibration info. Leave disabled--is controlled by quilt",
                MessageType.None
            );
        }
    }
}