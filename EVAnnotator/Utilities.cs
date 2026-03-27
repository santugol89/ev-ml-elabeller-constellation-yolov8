using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Net.NetworkInformation;
using System;

/// <summary>
/// Genie Utilities classes
/// </summary>
namespace GenieSupervisor
{
    /// <summary>
	/// Udupa
	/// </summary>
	public class Utilities
    {
        [DllImport("GenieLib.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        static extern int LogMessage(int tab, StringBuilder messageText);

        public static void LogMessage(string messageText, int tab = 0)
        {
            LogMessage(new StringBuilder(messageText), tab);
        }

        public static void LogMessage(StringBuilder messageText, int tab = 0)
        {
            LogMessage(tab, messageText);
            //string strFile = @"C:\EVAnnotator\";
            //if (!Directory.Exists(strFile))
            //    Directory.CreateDirectory(strFile);
            //File.AppendAllText(strFile + @"\DebugLog.txt", messageText.ToString() + "\n");
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetDiskFreeSpaceEx(string lpDirectoryName,
        out ulong lpFreeBytesAvailable,
        out ulong lpTotalNumberOfBytes,
        out ulong lpTotalNumberOfFreeBytes);

        //! check if stats path disk space left < thresh (environment variable)
        public static bool CheckDiskSpaceOK(string DestinationDirectory, ulong SourceImageSize)
        {
            ulong FreeBytesAvailable;
            ulong TotalNumberOfBytes;
            ulong TotalNumberOfFreeBytes;

            bool success = GetDiskFreeSpaceEx(DestinationDirectory,
                      out FreeBytesAvailable,
                      out TotalNumberOfBytes,
                      out TotalNumberOfFreeBytes);
            if (!success)
            {
                return false;
                throw new System.ComponentModel.Win32Exception();
            }

            if (TotalNumberOfFreeBytes >= (ulong)SourceImageSize)
                return true;

            return false;
        }

        public static string GenerateToken(int length)
        {
            const string allowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            byte[] randomBytes = new byte[length];

            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(randomBytes);
            }

            StringBuilder token = new StringBuilder(length);

            foreach (byte byteValue in randomBytes)
            {
                token.Append(allowedChars[byteValue % allowedChars.Length]);
            }

            return token.ToString();
        }

        public static string GetMacAddress()
        {
            string macAddress = string.Empty;

            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface networkInterface in networkInterfaces)
            {
                if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet)  //|| networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211
                {
                    macAddress = networkInterface.GetPhysicalAddress().ToString();
                    break;
                }
            }

            return macAddress;
        }

        public static void Shuffle<T>(List<T> list)
        {
            Random random = new Random();
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(0, i + 1); // Random index from 0 to i
                (list[i], list[j]) = (list[j], list[i]); // Swap elements
            }
        }
    }

    public class IniFile
    {
        private string filePath;

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        /// IniFile Constructor.
        public IniFile(string iniPath, bool flush = false)
        {
            filePath = iniPath;
            if (flush)
                File.Delete(filePath);
        }
        /// Write Data to the Ini File
        public void WriteValue<T>(string section, string key, T val)
        {
            WritePrivateProfileString(section, key, val.ToString(), filePath);
        }

        /// Read Data Value From the Ini File
        public T ReadValue<T>(string section, string key, T defaultVal)
        {
            StringBuilder str = new StringBuilder(255);
            int status = GetPrivateProfileString(section, key, "", str, 255, filePath);
            try {
                string str1 = str.ToString();
                if (!string.IsNullOrEmpty(str1))
                    return (T)System.Convert.ChangeType(str1, typeof(T));
            }
            catch { }

            return defaultVal;
        }
    }

    public class UndoRedoClass<T>
    {
        private Stack<T> UndoStack;
        private Stack<T> RedoStack;

        public T _currentItem;
        public UndoRedoClass(/*T currentItem*/)
        {
            UndoStack = new Stack<T>();
            RedoStack = new Stack<T>();
           // _currentItem = currentItem;
        }

        public void Clear()
        {
            UndoStack.Clear();
            RedoStack.Clear();
        }

        public void InsertRedoStack(T currentItem)
        {
            RedoStack.Push(currentItem);
        }

        public void InsertUndoStack(T currentItem)
        {
            UndoStack.Push(currentItem);
        }

        public T UndoObject
        {
            get {
                _currentItem = UndoStack.Pop();
                return _currentItem;
            }            
        }

        public T RedoObject
        {
            get {
                _currentItem = RedoStack.Pop();
                return _currentItem;
            }            
        }

        public bool CanUndo()
        {
            return UndoStack.Count > 0 ? true : false;
        }

        public bool CanRedo()
        {
            return RedoStack.Count > 0 ? true : false;
        }

        public int UndoCount()
        {
            return UndoStack.Count;
        }

        public int RedoCount()
        {
            return RedoStack.Count;
        }
    }

    public class UndoRedoItem
    {
        public string Type { get; set; }

        public List<object> listObjects { get; set; }

        public UndoRedoItem()
        {
            listObjects = new List<object>();
        }
    }

    public static class ExtensionsForInt32
    {
        public static bool IsInBetween(this double x, double a, double b)
        {
            return (x - a) * (x - b) < 0;
        }
    }
}

