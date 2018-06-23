//Copyright 2017 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.Events;

namespace HoloPlay
{
    [ExecuteInEditMode]
    public static class Config
    {
        [Serializable]
        public class ConfigValue
        {
            public readonly bool isInt;
            [SerializeField]
            float value;
            public float Value
            {
                get { return value; }
                set
                {
                    this.value = isInt ? Mathf.Round(value) : value;
                    this.value = Mathf.Clamp(this.value, min, max);
                }
            }
            public readonly float defaultValue;
            public readonly float min;
            public readonly float max;
            public readonly string name;
            public ConfigValue(float defaultValue, float min, float max, string name, bool isInt = false)
            {
                this.defaultValue = defaultValue;
                this.min = min;
                this.max = max;
                this.Value = defaultValue;
                this.name = name;
                this.isInt = isInt;
            }

            // just to make life easier
            public int asInt { get { return (int)value; } }
            public bool asBool { get { return (int)value == 1; } }

            public static implicit operator float(ConfigValue configValue)
            {
                return configValue.Value;
            }
        }

        [Serializable]
        /// <summary>
        /// Type for visual lenticular calibration
        /// </summary>
        public class VisualConfig
        {
            public float configVersion = 0.4f;
            public ConfigValue pitch = new ConfigValue(49.91f, 1f, 200, "Pitch");
            public ConfigValue slope = new ConfigValue(5.8f, -30, 30, "Slope");
            public ConfigValue center = new ConfigValue(0, -1, 1, "Center");
            public ConfigValue viewCone = new ConfigValue(40, 0, 180, "View Cone");
            public ConfigValue invView = new ConfigValue(0, 0, 1, "View Inversion", true);
            public ConfigValue verticalAngle = new ConfigValue(0, -20, 20, "Vert Angle");
            public ConfigValue DPI = new ConfigValue(338, 1, 1000, "DPI", true);
            public ConfigValue screenW = new ConfigValue(2560, 640, 6400, "Screen Width", true);
            public ConfigValue screenH = new ConfigValue(1600, 480, 4800, "Screen Height", true);
            public ConfigValue flipImageX = new ConfigValue(0, 0, 1, "Flip Image X", true);
            public ConfigValue flipImageY = new ConfigValue(0, 0, 1, "Flip Image Y", true);
            public ConfigValue flipSubp = new ConfigValue(0, 0, 1, "Flip Subpixels", true);
            [NonSerialized] public string loadedFrom = "not loaded -- default used";
            [NonSerialized] public bool loadedSuccess = false;
            // ? add quilt settings here for default quilt settings, but make this able to be overwritten by a more local file
            // the solution for now is just to keep the default settings for the quilt at High Res
        }

        //**********/
        //* fields */
        //**********/

        // ! warning ! //
        // don't use this
        private static VisualConfig instance;
        /// <summary>
        /// Try to avoid using this! It's meant to keep certain older frameworks functional.
        /// Try to refer to the config loaded by an instance of Quilt instead.
        /// </summary>
        public static VisualConfig Instance
        {
            get
            {
                if (instance == null)
                {
                    LoadVisualFromFile(out instance, visualFileNames[0]);
#if CALIBRATOR
                    Quilt.Instance.config = instance;
#endif
                }
                return instance;
            }
            set
            {
                instance = value;
            }
        }

        //* paths for calibration */
        // public readonly static string visualFileName = "visual.json";
        public readonly static string[] visualFileNames = new string[]{
            "visual.json",
            "visual2.json",
            "visual3.json",
            "visual4.json",
        };
        // public readonly static string quiltFileName = "quilt.json";
#if MULTI_LKG
        public readonly static string configDirName = "LKG_calibration_multi";
#else
        public readonly static string configDirName = "LKG_calibration";
#endif
        public readonly static string relativeConfigPath = Path.Combine(configDirName, visualFileNames[0]);

        //***********/
        //* methods */
        //***********/

        public static ConfigValue[] EnumerateConfigFields(VisualConfig visualConfig)
        {
            System.Reflection.FieldInfo[] configFields = typeof(Config.VisualConfig).GetFields();
            // List<System.Reflection.FieldInfo> configFieldsList = new List<System.Reflection.FieldInfo>();
            List<ConfigValue> configValues = new List<ConfigValue>();
            for (int i = 0; i < configFields.Length; i++)
            {
                if (configFields[i].FieldType == typeof(Config.ConfigValue))
                {
                    // configFieldsList.Add(configFields[i]);
                    Config.ConfigValue val = (Config.ConfigValue)configFields[i].GetValue(visualConfig);
                    configValues.Add(val);
                }
            }
            return configValues.ToArray();
        }

        public static string FormatPathToOS(string path)
        {
            path = path.Replace('\\', Path.DirectorySeparatorChar);
            path = path.Replace('/', Path.DirectorySeparatorChar);
            return path;
        }

        public static bool GetDoesConfigFileExist(string relativePathToConfig)
        {
            string temp;
            return GetConfigPathToFile(relativePathToConfig, out temp);
        }

        //this method is used to figure out which drive is the usb flash drive is related to HoloPlayer, and then returns that path so that our settings can load normally from there.
        public static bool GetConfigPathToFile(string relativePathToConfig, out string fullPath)
        {
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
            {
                relativePathToConfig = FormatPathToOS(relativePathToConfig);

                string[] drives = System.Environment.GetLogicalDrives();
                foreach (string drive in drives)
                {
                    if (File.Exists(drive + relativePathToConfig))
                    {
                        fullPath = drive + relativePathToConfig;
                        return true;
                    }
                }
            }
            else  //osx,  TODO: linux untested in standalone
            {
                string[] directories = Directory.GetDirectories("/Volumes/");
                foreach (string d in directories)
                {
                    string fixedPath = d + "/" + relativePathToConfig;
                    fixedPath = FormatPathToOS(fixedPath);

                    FileInfo f = new FileInfo(fixedPath);
                    if (f.Exists)
                    {
                        fullPath = f.FullName;
                        return true;
                    }
                }
            }
            fullPath = Path.GetFileName(relativePathToConfig); //return the base name of the file only.
            return false;
        }

        public static void SaveVisualToFile(VisualConfig configToSave, string fileName, bool eeprom = true)
        {
#if EEPROM_CALIB_ENABLE
            if (eeprom)
            {
                /****** save config to EEPROM ******/
                int ret = EEPROMCalibration.WriteConfigToEEPROM(configToSave);
                if (ret == 0)
                {
                    // EEPROM saving was successful
                    Debug.Log(Misc.warningText + "Calibration saved to memory on device.");
                    return;
                }
                else
                {
                    // EEPROM saving was unsuccesful.
                    Debug.Log(Misc.warningText + "Onboard calibration save failed.");
                }
            }
#endif
            string filePath;
            if (!GetConfigPathToFile(Path.Combine(configDirName, fileName), out filePath))
            {
                // ? throw a big, in-game visible warning if this fails
                Debug.LogWarning(Misc.warningText + "Unable to save config!");
                return;
            }
            // Debug.Log(filePath + " \n is the filepath");

            string json = JsonUtility.ToJson(configToSave, true);

            File.WriteAllText(filePath, json);
            Debug.Log(Misc.warningText + "Config saved!");

#if UNITY_EDITOR
            if (UnityEditor.PlayerSettings.defaultScreenWidth != configToSave.screenW.asInt ||
                UnityEditor.PlayerSettings.defaultScreenHeight != configToSave.screenH.asInt)
            {
                UnityEditor.PlayerSettings.defaultScreenWidth = configToSave.screenW.asInt;
                UnityEditor.PlayerSettings.defaultScreenHeight = configToSave.screenH.asInt;
            }
#endif
        }

        /// <summary>
        /// Loads a config
        /// </summary>
        /// <param name="loadedConfig">the config to populate</param>
        /// <returns>true if successfully loaded, otherwise false</returns>
        public static bool LoadVisualFromFile(out VisualConfig loadedConfig, string fileName)
        {
            loadedConfig = new VisualConfig();
            bool fileExists = false;
            string filePath;
            bool eepromFound = false;
#if EEPROM_CALIB_ENABLE
            int ret = EEPROMCalibration.LoadConfigFromEEPROM(ref loadedConfig);
            if (ret == 0)
            {
                Debug.Log(Misc.warningText + "Config file loaded from device memory.");
                loadedConfig.loadedFrom = "Device memory";
                // calibration loaded successfully!
                // EEPROMCalibration.PrintConfig(hpc);
                eepromFound = true;
            }
            else if (ret < -5)
            {
                // if ret = -6, the calibration was loaded but the version number didn't match
                // if ret = -5, data was loaded from EEPROM but not recognized as a calibration file
                //     (this will happen whenever the HoloPlayConfig changes in size, for instance if you change a float to an int)
            }
            if (ret < 0)
            {
                // if ret = -4, -3, -2, or -1, we couldn't open the HID pipe
            }
#endif
            if (!eepromFound)
            {
                if (!GetConfigPathToFile(Path.Combine(configDirName, fileName), out filePath))
                {
                    Debug.LogWarning(Misc.warningText + "Config file not found!");
                }
                else
                {
                    string configStr = File.ReadAllText(filePath);
                    if (configStr.IndexOf('{') < 0 || configStr.IndexOf('}') < 0)
                    {
                        // if the file exists but is unpopulated by any info, don't try to parse it
                        // this is a bug with jsonUtility that it doesn't know how to handle a fully empty text file >:(
                        Debug.LogWarning(Misc.warningText + "Config file not found!");
                    }
                    else
                    {
                        // if it's made it this far, just load it
                        fileExists = true;
                        Debug.Log(Misc.warningText + "Config loaded! loaded from " + filePath);
                        loadedConfig = JsonUtility.FromJson<VisualConfig>(configStr);
                        loadedConfig.loadedFrom = filePath;
                    }
                }
            }
            // make sure test value is always 0 unless specified by calibrator
            // inverted viewcone is handled separately now, so just take the abs of it
            loadedConfig.viewCone.Value = Mathf.Abs(loadedConfig.viewCone.Value);

            // note: instance static ref is legacy
            instance = loadedConfig;

            return fileExists || eepromFound;
        }

    }
}