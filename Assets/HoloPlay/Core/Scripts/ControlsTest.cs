using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloPlay;

namespace HoloPlaySDK_Tests
{
    public class ControlsTest : MonoBehaviour
    {
        void OnGUI()
        {
            GUI.skin.box.fontSize = 50;

            var buttons = new HPButtonType[]{
                HPButtonType.ONE,
                HPButtonType.TWO,
                HPButtonType.THREE,
                HPButtonType.FOUR,
                HPButtonType.HOME
            };

            foreach (var b in buttons)
            {
                if (HPButton.GetButton(b))
                    GUILayout.Box("HP Button " + b.ToString());
            }
        }
    }
}