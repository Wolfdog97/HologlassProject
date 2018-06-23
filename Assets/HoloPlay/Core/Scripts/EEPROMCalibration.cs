#if EEPROM_CALIB_ENABLE
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

namespace HoloPlay
{

    public class EEPROMCalibration
    {
        private static bool debugprints = false;
        private const int USB_VID = 0x2341;
        private const int USB_PID = 0x8036;
        /* DO NOT CHANGE ANY OF THE FOLLOWING VALUES!*/
        private const byte pagelen = 64;
        private const byte sendlen = pagelen + 2;
        private const byte recvlen = pagelen + 2;
        private const byte max_addr = 1024 / pagelen;
        private const byte EEPROM_START_BYTE = 0x69;
        private const byte EEPROM_END_BYTE = 0x42;
        private const byte EEPROM_START_PAGE = 0x0;
        /********************************************/

        /*private static void Test()
        {
            debugprints = true;
            int ret = 0;

            // test read
            print("Reading config...");
            Config.VisualConfig hpc0 = new Config.VisualConfig();
            ret = LoadConfigFromEEPROM(ref hpc0);
            if (ret == 0) PrintConfig(hpc0);

            // test write
            Config.VisualConfig hpc = new Config.VisualConfig();
            hpc.center.Value = 0f;
            hpc.pitch.Value = 6.96969696f;
            hpc.DPI.Value = 1.23456789f;
            hpc.viewCone.Value = 12.3456789f;
            hpc.verticalAngle.Value = 123.456789f;
            hpc.numViews.Value = 1234.56789f;
            hpc.screenH.Value = 500.69f;
            hpc.configVersion = 0.1f;
            print("Writing config...");
            PrintConfig(hpc);
            ret = WriteConfigToEEPROM(hpc);

            // test read
            Config.VisualConfig hpc2 = new Config.VisualConfig();
            print("Rereading config...");
            ret = LoadConfigFromEEPROM(ref hpc2);
            if (ret == 0) PrintConfig(hpc2);
        }*/

        public static void PrintConfig(Config.VisualConfig h)
        {
            String s = "";
            FieldInfo[] fields = h.GetType().GetFields();

            foreach (FieldInfo field in fields)
            {
                if (field != null)
                {
                    object val = field.GetValue(h);
                    if (val is float)
                    {
                        s += String.Format("{0:G}f ", (float)val);
                    }
                    else if (val is Config.ConfigValue)
                    {
                        Config.ConfigValue val_ = (Config.ConfigValue)val;
                        if (val_.isInt)
                        {
                            s += String.Format("{0:D}i ", (Int16)val_);
                        }
                        else
                        {
                            s += String.Format("{0:G}f ", (float)val_);
                        }
                    }
                }
            }
            print(s);
        }

        private static byte[] SerializeHoloPlayConfig(Config.VisualConfig hpc)
        {
            List<byte> byte_out = new List<byte>();
            FieldInfo[] fields = hpc.GetType().GetFields();
            foreach (FieldInfo field in fields)
            {
                if (field != null)
                {
                    object val = field.GetValue(hpc);
                    if (val is float)
                    {
                        byte[] bytes = BitConverter.GetBytes((float)val);
                        foreach (byte b in bytes) byte_out.Add(b);
                    }
                    else if (val is Config.ConfigValue)
                    {
                        Config.ConfigValue val_ = (Config.ConfigValue)val;
                        byte[] bytes;
                        if (val_.isInt)
                        {
                            bytes = BitConverter.GetBytes((Int16)val_);
                        }
                        else
                        {
                            bytes = BitConverter.GetBytes((float)val_);
                        }
                        foreach (byte b in bytes) byte_out.Add(b);
                    }
                }
            }
            return byte_out.ToArray();
        }
        private static int BytesInHPC(Config.VisualConfig hpc)
        {
            int bytes_in_hpc_ = 0;
            int intlen = sizeof(Int16);
            int floatlen = sizeof(float);
            FieldInfo[] fields = hpc.GetType().GetFields();
            foreach (FieldInfo field in fields)
            {
                if (field != null)
                {
                    object val = field.GetValue(hpc);
                    if (val is float)
                    {
                        bytes_in_hpc_ += floatlen;
                    }
                    else if (val is Config.ConfigValue)
                    {
                        Config.ConfigValue val_ = (Config.ConfigValue)val;
                        if (val_.isInt)
                        {
                            bytes_in_hpc_ += intlen;
                        }
                        else
                        {
                            bytes_in_hpc_ += floatlen;
                        }
                    }
                }
            }
            return bytes_in_hpc_;
        }

        private static Config.VisualConfig DeserializeHoloPlayConfig(byte[] byte_in)
        {
            Config.VisualConfig hpc = new Config.VisualConfig();
            if (byte_in.Length != BytesInHPC(hpc))
            {
                printerr("HoloPlayConfig length mismatch! Aborting...");
                return null;
            }
            int intlen = sizeof(Int16);
            int floatlen = sizeof(float);
            int ind = 0;
            FieldInfo[] fields = hpc.GetType().GetFields();
            foreach (FieldInfo field in fields)
            {
                if (field != null)
                {
                    object val = field.GetValue(hpc);
                    if (val is float)
                    {
                        field.SetValue(hpc, BitConverter.ToSingle(byte_in, ind));
                        ind += floatlen;
                    }
                    else if (val is Config.ConfigValue)
                    {
                        Config.ConfigValue val_ = (Config.ConfigValue)val;
                        if (val_.isInt)
                        {
                            val_.Value = BitConverter.ToInt16(byte_in, ind);
                            ind += intlen;
                        }
                        else
                        {
                            val_.Value = BitConverter.ToSingle(byte_in, ind);
                            ind += floatlen;
                        }
                    }
                }
            }
            return hpc;
        }

        public static int LoadConfigFromEEPROM(ref Config.VisualConfig hpc)
        {
            float ver = hpc.configVersion;
            byte[] bytes_read = new byte[BytesInHPC(hpc)];
            for (int i = 0; i < bytes_read.Length; ++i) bytes_read[i] = 0;
            byte[] in_buf = new byte[bytes_read.Length + 2];
            int err = rw((byte)EEPROM_START_PAGE, in_buf.Length, in_buf, true);
            if (err == 0)
            {
                if (in_buf[0] == EEPROM_START_BYTE && in_buf[in_buf.Length - 1] == EEPROM_END_BYTE)
                {
                    for (int i = 0; i < bytes_read.Length; ++i)
                    {
                        bytes_read[i] = in_buf[i + 1];
                    }
                }
                else { err = -5; }
            }
            if (err != 0)
            {
                printerr(String.Format("Error {0:D}: HoloPlay Config could not be loaded from EEPROM! Using default HoloPlay Config.", -1 * err));
                return err;
            }
            hpc = DeserializeHoloPlayConfig(bytes_read);
            if (hpc == null) return -7;
            if (hpc.configVersion != ver)
            {
                printerr(String.Format("HoloPlay Config version mismatch. Version expected: {0:G}; version found: {1:G}. Using default HoloPlay Config. Please recalibrate.", ver, hpc.configVersion));
                return -6;
            }
            return 0;
        }
        private static void print(String s, bool err = false)
        {
            if (debugprints) Debug.Log(s);
        }
        private static void printerr(String s)
        {
            if (debugprints) Debug.LogError(s);
        }
        public static int WriteConfigToEEPROM(Config.VisualConfig hpc)
        {
            print("Writing config to EEPROM, please wait...");
            PrintConfig(hpc);
            byte[] in_buf = SerializeHoloPlayConfig(hpc);
            byte[] bytes_out = new byte[in_buf.Length + 2];
            bytes_out[0] = EEPROM_START_BYTE;
            bytes_out[bytes_out.Length - 1] = EEPROM_END_BYTE;
            for (int i = 0; i < in_buf.Length; ++i) bytes_out[i + 1] = in_buf[i];
            int err = rw((byte)EEPROM_START_PAGE, bytes_out.Length, bytes_out, false);
            if (err != 0)
            {
                printerr(String.Format("Error {0:D}: HoloPlay Config could not be written to EEPROM! Using default HoloPlay Config.", -1 * err));
                return err;
            }
            print("Config successfully written to EEPROM!");
            return err;
        }

        private static int rw(byte start_page, int bytes, byte[] in_buf, bool read = true)
        {
            bool connect_success = true;
            IntPtr ptr = HIDapi.hid_enumerate(USB_VID, USB_PID);
            if (ptr == IntPtr.Zero)
            {
                HIDapi.hid_free_enumeration(ptr);
                connect_success = false;
                return -4;
            }
            hid_device_info enumerate = (hid_device_info)Marshal.PtrToStructure(ptr, typeof(hid_device_info));
            IntPtr handle = HIDapi.hid_open_path(enumerate.path);
            HIDapi.hid_set_nonblocking(handle, 1);
            HIDapi.hid_free_enumeration(ptr);
            if (!connect_success)
            {
                HIDapi.hid_close(handle);
                return -4;
            }
            int numPages = bytes / pagelen;
            if (bytes % pagelen != 0) ++numPages;
            int in_buf_ind = 0;
            byte[] r = new byte[recvlen];
            byte[] s = new byte[sendlen];
            for (int i = start_page; i < start_page + numPages; ++i)
            {
                byte cmd = (byte)(read ? i & ~0x80 : i | 0x80);
                if (!read)
                {
                    for (int j = 2; j < sendlen; ++j)
                    {
                        if (in_buf_ind < bytes)
                        {
                            s[j] = in_buf[in_buf_ind];
                            ++in_buf_ind;
                        }
                    }
                }
                s[0] = 0;
                s[1] = cmd;
                int res = 0;
                int err = 0;
                res = HIDapi.hid_send_feature_report(handle, s, new UIntPtr(sendlen));
                if (res < 1)
                {
                    err = -1;
                }
                else
                {
                    res = HIDapi.hid_read_timeout(handle, r, new UIntPtr(recvlen), 1000);
                    if (res < 1)
                    {
                        err = -2;
                    }
                    if (r[1] != s[1])
                    {
                        err = -3;
                    }
                }
                if (err == 0)
                {
                    if (read)
                    {
                        for (int j = 2; j < recvlen; ++j)
                        {
                            if (in_buf_ind < bytes)
                            {
                                in_buf[in_buf_ind] = r[j];
                                ++in_buf_ind;
                            }
                        }
                    }
                }
                else
                {
                    HIDapi.hid_close(handle);
                    return err;
                }
                string formatstr=string.Concat(String.Format("page {0:D}, read: {1:B}, data: ", i, read),"{0:S}");
                PrintArray(r, format:formatstr);
            }
            HIDapi.hid_close(handle);
            return 0;
        }
        private static int rwPage(IntPtr handle, byte address, byte[] s, byte[] r)
        {
            int res = 0;
            s[0] = 0;
            s[1] = address;
            res = HIDapi.hid_send_feature_report(handle, s, new UIntPtr(sendlen));
            if (res < 1) return -1;
            res = HIDapi.hid_read_timeout(handle, r, new UIntPtr(recvlen), 1000);
            if (res < 1) return -2;
            if (r[1] != s[1])
            {
                return -3;
            }
            return 0;
        }

        private static void PrintArray<T>(T[] arr, uint len = 0, uint start = 0, string format = "{0:S}")
        {
            if (len == 0) len = (uint)arr.Length;
            string tostr = "";
            for (int i = 0; i < len; ++i)
            {
                tostr += string.Format((arr[0] is byte) ? "{0:X2} " : ((arr[0] is float) ? "{0:G} " : "{0:D} "), arr[i + start]);
            }
            print(string.Format(format, tostr));
        }
    }
}
#endif