using System;

namespace Stash.Discover
{
    public static class Logger
    {
        public const string LOG_OUTPUT_FILE = "stashclient.log";
        public const string LOG_DIR = "Stash";          // This is appended to the LocalApplicationData directory
        public const string LOG_SUBDIR = "SyncClient";      // This is appended to the LocalApplicationData/LOG_DIR directory
        public static object writeLogLock = new object();

        [Flags]
        public enum LogDetail
        {
            None = 1,
            Critical = 2,
            Error = 4,
            Status = 32,
            StatusVerbose = 64,
            Debug = 512,
            All = 1024,
        }

        public static void LogItem(string msgIn, LogDetail logType, Exception exIn)
        {
            // Console Logging when in Debug mode
            DateTime curTime = DateTime.UtcNow;
            string LogOutputString = "";
            //System.IO.StreamWriter w = null;

            try
            {
                LogOutputString = curTime.ToString() + " - " + SupportFn.ConvertTSToEpoch(curTime);
                LogOutputString += " - " + msgIn;
                Console.WriteLine(LogOutputString);
            } catch (Exception ex) {
                Console.WriteLine("Error Writing to Console Output - " + ex.Message);
            }

            //// Log to a file
            //try
            //{
            //    // Get application storage directory
            //    //string LogFilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), STASHClient.CLIENT_DIRECTORY);
            //    string LogFilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Logger.LOG_DIR, Logger.LOG_SUBDIR);

            //    System.IO.DirectoryInfo logFileDir = new System.IO.DirectoryInfo(LogFilePath);
            //    if (!logFileDir.Exists)
            //    {
            //        logFileDir.Create();
            //    }

            //    string LogFile = System.IO.Path.Combine(LogFilePath, LOG_OUTPUT_FILE);
            //    LogOutputString = curTime.ToString() + " - " + SupportFn.ConvertTSToEpoch(curTime);
            //    LogOutputString += " - " + msgIn;

            //    // Include stack trace if its a critical error
            //    if (exIn != null && logType == LogDetail.Critical)
            //    {
            //        LogOutputString += exIn.StackTrace;
            //    }

            //    AttemptMultiWrite(LogFile, LogOutputString);        // Try to open and write to log file mutliple times
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine("Error Writing to Log Output File - " + ex.Message);
            //}
            //finally
            //{
            //    if (w != null)
            //    {
            //        w.Dispose();
            //    }
            //}
        }

        //public static void AttemptMultiWrite(string LogFile, string strOutput)
        //{
        //    // Function attempts to write the output string to a stream 
        //    int counter = 0;
        //    System.IO.StreamWriter w = null;
        //    while (counter < 5)
        //    {
        //        try
        //        {
        //            lock (writeLogLock)
        //            {
        //                using (w = new System.IO.StreamWriter(LogFile, true))
        //                {
        //                    w.WriteLine(strOutput);
        //                    // w.WriteLine(LogOutputString);
        //                    //w.Dispose();
        //                }
        //            }
        //            break;
        //        }
        //        //catch (System.IO.IOException exIO)
        //        //{
        //        //    counter = counter + 1;
        //        //    System.Threading.Thread.Sleep(50);
        //        //}
        //        catch (Exception ex)
        //        {
        //            throw new Exception(ex.Message);
        //        }
        //    }
        //}
    }
}
