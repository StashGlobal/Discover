using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.Win32;
using System.Reflection;

namespace Stash.Discover
{
    public static class SupportFn
    {
        public static string StripUriHeader(string strIn)
        {
            string uriHeader = "file:///";
            if (strIn.StartsWith(uriHeader))
            {
                return strIn.Remove(0, uriHeader.Length);
            }
            else
            {
                return "";
            }
        }

        //public static void AddApplicationToStartup()
        //{
        //    using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
        //    {
        //        if (key.GetValue(Globals.CLIENT_STARTUP_REGKEY) == null)
        //        {
        //            key.SetValue(Globals.CLIENT_STARTUP_REGKEY, "\"" + SupportFn.StripUriHeader(System.Reflection.Assembly.GetExecutingAssembly().CodeBase) + "\"");
        //        }
        //    }
        //}

        //public static void RemoveApplicationFromStartup()
        //{
        //    using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
        //    {
        //        if (key.GetValue(Globals.CLIENT_STARTUP_REGKEY) != null)
        //        {
        //            key.DeleteValue(Globals.CLIENT_STARTUP_REGKEY, false);
        //        }
        //    }
        //}

        /* Converts the input number of bytes to a human readable form
         * 
         */
        public static string ConvertToReadableFileSize(ulong sizeIn)
        {
            Logger.LogItem("STATUS - StashClientApi.cs:ConvertToReadableFileSize() - Start, sizeIn: " + sizeIn.ToString(), Logger.LogDetail.Status, null);
            try
            {
                double tSize = 0;

                if (sizeIn < 1024)                  // Bytes
                {
                    return sizeIn.ToString() + " bytes";
                }
                if (sizeIn < 1048576)               // KBytes
                {
                    tSize = (double)sizeIn / 1024;
                    return Math.Round(tSize, 2) + " kB";
                }
                if (sizeIn < 1073741824)            // MBytes
                {
                    tSize = (double)sizeIn / 1048576;
                    return Math.Round(tSize, 2) + " MB";
                }
                if (sizeIn < 1099511627776)         // GBytes
                {
                    tSize = (double)sizeIn / 1073741824;
                    return Math.Round(tSize, 2) + " GB";
                }
                tSize = (double)sizeIn / 1099511627776;
                return Math.Round(tSize, 2) + " TB";     // Terrabytes
            }
            catch (Exception ex)
            {
                Logger.LogItem("ERROR - StashClientApi.cs:ConvertToReadableFileSize() - Error - " + ex.Message, Logger.LogDetail.Error, null);
                return "Error";
            }
        }

        /* Converts a ulong number of seconds to a Hrs/Mins/Secs string
         */
        public static string ConvertSecondsToReadableTime(ulong secondsIn)
        {
            Logger.LogItem("STATUS - StashClientApi.cs:ConvertSecondsToReadableTime() - Start, secondsIn: " + secondsIn.ToString(), Logger.LogDetail.Status, null);

            try
            {
                if (secondsIn <= 0)
                {
                    return "None";
                }
                ulong numHours = secondsIn / 3600;
                ulong numMinutes = (secondsIn % 3600) / 60;
                if (numMinutes > 59)
                {
                    numMinutes = 59;
                }
                ulong numSeconds = secondsIn % 60;
                if (numSeconds > 59)
                {
                    numSeconds = 59;
                }

                return numHours.ToString() + " Hrs. " + numMinutes.ToString() + " Mins. " + numSeconds.ToString() + " Secs.";
            }
            catch (Exception ex)
            {
                Logger.LogItem("ERROR - StashClientApi.cs:ConvertSecondsToReadableTime() - Error - " + ex.Message, Logger.LogDetail.Error, null);
                return "Calculating Time Remaining...";
            }
        }

        public static void RemoveTrailingSlash(ref string strIn)
        {
            strIn = strIn.Remove(strIn.Length - 1);
        }

        public static void AddTrailingSlash(ref string strIn)
        {
            if (!strIn.EndsWith(Path.PathSeparator.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                strIn = String.Concat(strIn, Path.PathSeparator.ToString());                
            }
        }

        // Adds an element to the front of an array
        // See StashClientApi.cs for original source
        public static string[] PrependArray(string[] arrayIn, string prependValue)
        {
            string[] newArray = null;
            try
            {
                newArray = new string[arrayIn.Length + 1];
                newArray[0] = prependValue;
                Array.Copy(arrayIn, 0, newArray, 1, arrayIn.Length);
            }
            catch (Exception)
            {
                newArray = null;
            }
            return newArray;
        }

        // Converts a Windows Timestamp to Epoch time
        public static int ConvertTSToEpoch(DateTime dateIn)
        {
            try
            {
                var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                return Convert.ToInt32((dateIn - epoch).TotalSeconds);
            }
            catch (Exception ex)
            {
                Logger.LogItem("STATUS - StashClientApi.cs:ConvertTSToEpoch() - Error - " + ex.Message, Logger.LogDetail.Status, null);
                return 0;
            }
        }

        // Converts an Epoch time to a Windows timestamp
        public static DateTime ConvertEpochToTS(ulong epochIn)
        {
            try
            {
                var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                return epoch.AddSeconds(epochIn);
            }
            catch (Exception)
            {
                return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }
        }

        public static DateTime ConvertEpochToTS(int epochIn)
        {
            try
            {
                var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                return epoch.AddSeconds(epochIn);
            }
            catch (Exception ex)
            {
                Logger.LogItem("STATUS - StashClientApi.cs:ConvertEpochToTS() - Error - " + ex.Message, Logger.LogDetail.Status, null);
                return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }
        }

        public static string Hash(string input)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder(hash.Length * 2);

                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }

                return sb.ToString();
            }
        }

        public static string getDirName(string pathIn)
        {
            string[] pathArray = pathIn.Split(Path.PathSeparator.ToString());
            string fullPath = Path.PathSeparator.ToString();

            try
            {
                for (int i = 0; i < pathArray.Length - 1; i++)
                {
                    fullPath += pathArray[i];
                }
                return fullPath;
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static string getFileName(string pathIn)
        {
            try
            {
                return System.IO.Path.GetFileName(pathIn);
            }
            catch (Exception)
            {
                return "";
            }
        }

        //public static string EncryptDataDPAPI(string clearText, string optionalEntropy = null, DataProtectionScope scope = DataProtectionScope.CurrentUser)
        //{
        //    if (clearText == null)
        //        throw new ArgumentNullException("clearText");
        //    byte[] clearBytes = Encoding.UTF8.GetBytes(clearText);
        //    byte[] entropyBytes = string.IsNullOrEmpty(optionalEntropy)
        //        ? null
        //        : Encoding.UTF8.GetBytes(optionalEntropy);
        //    byte[] encryptedBytes = ProtectedData.Protect(clearBytes, entropyBytes, scope);
        //    return Convert.ToBase64String(encryptedBytes);
        //}

        //public static string DecryptDataDPAPI(string encryptedText, string optionalEntropy = null, DataProtectionScope scope = DataProtectionScope.CurrentUser)
        //{
        //    if (encryptedText == null)
        //        throw new ArgumentNullException("encryptedText");
        //    byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
        //    byte[] entropyBytes = string.IsNullOrEmpty(optionalEntropy)
        //        ? null
        //        : Encoding.UTF8.GetBytes(optionalEntropy);
        //    byte[] clearBytes = ProtectedData.Unprotect(encryptedBytes, entropyBytes, scope);
        //    return Encoding.UTF8.GetString(clearBytes);
        //}

        /* Converts the directory separators from one to another
         * Returns the modified string
        */
        //public static string ConvertToDirSeparator(string pathIn)
        //{
        //    string retVal = "";
        //    if (Globals.DIR_SEPARATOR == '/')
        //    {
        //        retVal = pathIn.Replace('\\', '/');
        //    }
        //    else if (Globals.DIR_SEPARATOR == '\\')
        //    {
        //        retVal = pathIn.Replace('/', '\\');
        //    }

        //    return retVal;
        //}
        
        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // From https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
            // Get the subdirectories for the specified directory.
            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new System.IO.DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            System.IO.DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!System.IO.Directory.Exists(destDirName))
            {
                System.IO.Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            System.IO.FileInfo[] files = dir.GetFiles();
            foreach (System.IO.FileInfo file in files)
            {
                string temppath = System.IO.Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (System.IO.DirectoryInfo subdir in dirs)
                {
                    string temppath = System.IO.Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        // Returns a string representing the directory with the highest version number
        // This is for determining which settings directory is the most recent (see STASHClient.cs::LoadSettings)
        public static string GetHighestVersionDirectory(string dirIn)
        {
            if (!System.IO.Directory.Exists(dirIn))
            {
                return "";
            }
            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(dirIn);

            // Get list of subdirectories with version numbers
            System.IO.DirectoryInfo[] subDirs = dir.GetDirectories();
            string verNum = "0.0.0.0";      // Starting version number
            foreach (System.IO.DirectoryInfo subDir in subDirs)
            {
                try
                {
                    if (Version.TryParse(subDir.Name, out Version result) && Version.Parse(subDir.Name) > Version.Parse(verNum))
                    {
                        verNum = subDir.Name;
                    }
                }
                catch (FormatException)
                {
                    // Skip directories that are not formatted as version numbers
                }
            }

            if (verNum != "0.0.0.0")
            {
                return verNum;
            }
            else
            {
                return "";
            }

        }
    }
}
