//Copyright 2017 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HoloPlay
{
    public enum HPButtonType
    {
        ONE = 0,
        TWO = 1,
        THREE = 2,
        FOUR = 3,
        HOME = 4,
    }

    public static class HPButton
    {
        // todo: stick this in quilt maybe
        // [Tooltip("Emulate HoloPlay buttons using keys 1-5. Shortcut to toggle this is F5")]
        // public bool emulateButtons;

        static int joystickNumber = -2;

        // balance checkInterval so it starts right away
        static float timeSinceLastCheck = -3f;

        static readonly float checkInterval = 3f;

        /// <summary>
        /// This happens automatically every x seconds as called from HoloPlay.
        /// No need for manually calling this function typically
        /// </summary>
        public static void ScanForHoloPlayerJoystick()
        {
            if (Time.unscaledTime - timeSinceLastCheck > checkInterval)
            {
                var joyNames = Input.GetJoystickNames();
                int i = 1;
                foreach (var joyName in joyNames)
                {
                    if (joyName.ToLower().Contains("holoplay"))
                    {
                        Debug.Log(Misc.warningText + "Found HID named: " + joyName);
                        joystickNumber = i; // for whatever reason unity starts their joystick list at 1 and not 0
                        return;
                    }
                    i++;
                }
                
                if (joystickNumber == -2)
                {
                    Debug.LogWarning(Misc.warningText + "No HoloPlay HID found");
                    joystickNumber = -1;
                }


                timeSinceLastCheck = Time.unscaledTime;
            }
        }

        public static bool GetButton(HPButtonType button)
        {
            return CheckButton((x) => UnityEngine.Input.GetKey(x), button);
        }

        public static bool GetButtonDown(HPButtonType button)
        {
            return CheckButton((x) => UnityEngine.Input.GetKeyDown(x), button);
        }

        public static bool GetButtonUp(HPButtonType button)
        {
            return CheckButton((x) => UnityEngine.Input.GetKeyUp(x), button);
        }

        /// <summary>
        /// Get any button down. By default, includeHome is false and it will only return on buttons 1-4
        /// </summary>
        public static bool GetAnyButtonDown(bool includeHome = false)
        {
            for (int i = 0; i < Enum.GetNames(typeof(HPButtonType)).Length; i++)
            {
                var button = (HPButtonType)i;
                if (includeHome && button == HPButtonType.HOME)
                    continue;

                if (GetButtonDown(button)) return true;
            }
            return false;
        }

        static bool CheckButton(Func<KeyCode, bool> buttonFunc, HPButtonType button)
        {
            bool buttonPress = buttonFunc(ButtonToNumberOnKeyboard(button));

            if (joystickNumber < 0)
            {
                ScanForHoloPlayerJoystick();
            }

            if (joystickNumber >= 0)
            {
                buttonPress = buttonPress || buttonFunc(ButtonToJoystickKeycode(button));
            }
            return buttonPress;
        }

        static KeyCode ButtonToJoystickKeycode(HPButtonType button)
        {
            return
                (KeyCode)Enum.Parse(
                    typeof(KeyCode),
                    "Joystick" + joystickNumber + "Button" + (int)button
                );
        }

        static KeyCode ButtonToNumberOnKeyboard(HPButtonType button)
        {
            switch (button)
            {
                case HPButtonType.ONE:
                    return KeyCode.Alpha1;
                case HPButtonType.TWO:
                    return KeyCode.Alpha2;
                case HPButtonType.THREE:
                    return KeyCode.Alpha3;
                case HPButtonType.FOUR:
                    return KeyCode.Alpha4;
                case HPButtonType.HOME:
                    return KeyCode.Alpha5;
                default:
                    return KeyCode.Alpha5;
            }
        }
    }
}