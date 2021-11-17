using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;                // For computed properties and notifications
using System.Runtime.CompilerServices;      // For computed properties and notifications
using System.Runtime.InteropServices;       // For Windows/Linux/MacOSX detection
using HeyRed.Mime;                          // For MIME Type detection

namespace Stash.Discover
{
    // Locates files using specified filter criteria
    class Scanner : INotifyPropertyChanged
    {
        private string _strCurrentDirPath = "";             // Tracks the current path the scanning is on
        private string _strCurrentFilePath = "";            // Tracks the current file & path the scanner is on
        private string _strCurrentFilePathMimeMatch = "";   // Tracks the current file which matches the scanner's configured MIME types
        private uint _intFileCount = 0;                     // Tracks the number of files this scanner instance has identified so far
        private string _strErrorMessage = "";               // Tracks non-fatal error messages for printing to screen
        private ConcurrentQueue<DiscoveredItem> cq = null;  // Pointer to the master queue managed by DiscoverMain
        private List<string> excludeDirectories = null;     // Stores the directories to ignore
        private List<string> fileTypes = null;              // Stores the file MIME types we are searching for

        // Default search files (MIME Types):
        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Basics_of_HTTP/MIME_types/Common_types
        // https://www.iana.org/assignments/media-types/media-types.xhtml
        //
        private static List<string> defaultFileTypes = new List<string> {
            "text/csv",
            "image/jpeg",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.ms-excel",
            "application/vnd.ms-powerpoint",
            "application/msword",
            "application/vnd.oasis.opendocument.text",
            "application/vnd.oasis.opendocument.spreadsheet",
            "application/vnd.oasis.opendocument.presentation",
            "application/pdf",
            "application/vnd.visio",
        };

        #region Public Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Computed Properties
        // When the scanner updates these properties, it will notify DiscoverMain, which then updates the display
        // These properties are monitored by Output.cs:UpdateDisplay() - the property change handlers are set in DiscoverMain.cs:Main()

        public string strCurrentDirPath
        {
            get { return this._strCurrentDirPath; }
            set
            {
                if (this._strCurrentDirPath != value)
                {
                    this._strCurrentDirPath = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string strCurrentFilePath
        {
            get { return this._strCurrentFilePath; }
            set
            {
                if (this._strCurrentFilePath != value)
                {
                    this._strCurrentFilePath = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string strCurrentFilePathMimeMatch
        {
            get { return this._strCurrentFilePathMimeMatch; }
            set
            {
                if (this._strCurrentFilePathMimeMatch != value)
                {
                    this._strCurrentFilePathMimeMatch = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public uint intFileCount
        {
            get { return this._intFileCount; }
            set
            {
                if (this._intFileCount != value)
                {
                    this._intFileCount = value;
                    NotifyPropertyChanged();
                }
            }
        }

        // Used to track error or warning messages that are not fatal
        public string strErrorMessage
        {
            get { return this._strErrorMessage; }
            set
            {
                if (this._strErrorMessage != value)
                {
                    this._strErrorMessage = value;
                    NotifyPropertyChanged();
                }
            }
        }
        #endregion

        // Set default file types - may be overriden by command line options
        public Scanner(ConcurrentQueue<DiscoveredItem> cq, string fileTypesIn, string excludeDirectoriesIn)
        {
            this.cq = cq;
            
            // Parse the list of file types to search for into a sorted List
            if (fileTypesIn != "")
            {
                this.fileTypes = fileTypesIn.Split(',').ToList();
            } else
            {
                this.fileTypes = Scanner.defaultFileTypes;
            }
            this.fileTypes.Sort();

            // Parse the list of excluded directories into a sorted List
            if (excludeDirectoriesIn != "") {
                this.excludeDirectories = excludeDirectoriesIn.Split(',').ToList();
                this.excludeDirectories.Sort();
            } else
            {
                this.excludeDirectories = new List<string>();
            }
        }

        public async Task Go(string pathIn)
        {
            if (pathIn == "")
            {
                // Enumerate the full file system
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // For windows, get all drives and enumerate directories and files on them
                    foreach (System.IO.DriveInfo driv in System.IO.DriveInfo.GetDrives())
                    {
                        if (driv.IsReady)
                        {
                            await this.Go(driv.RootDirectory.FullName);
                        }
                    }
                } else
                {
                    await this.Go(Path.PathSeparator.ToString());
                }
            } else
            {
                //if (this.excludeDirectories.Contains(pathIn)) { return; }       // Skip a directory if in the exclude list
                if (this.excludeDirectories.BinarySearch(pathIn) >= 0) { return; }

                // Get all files in the directory
                try
                {
                    System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(pathIn);
                    this.strCurrentDirPath = dirInfo.FullName;
                    foreach (System.IO.FileInfo fi in dirInfo.GetFiles())
                    {
                        // Get file MIME type and if its on the 'analyze' list, queue it
                        this.strCurrentFilePath = fi.FullName;
                        string strMime = MimeGuesser.GuessMimeType(fi);
                        if (this.fileTypes.BinarySearch(strMime) >= 0)
                        {
                            DiscoveredItem di = new DiscoveredItem(fi.FullName);
                            di.fileMimeType = strMime;
                            this.cq.Enqueue(di);
                            Monitor.Enter(this);
                            this.intFileCount++;
                            Monitor.Exit(this);
                            this.strCurrentFilePathMimeMatch = fi.FullName;
                        }
                    }

                    // Recurse on all subdirectories in the directory
                    foreach (System.IO.DirectoryInfo subDirInfo in dirInfo.GetDirectories())
                    {
                        await this.Go(subDirInfo.FullName);
                    }
                }
                catch (System.UnauthorizedAccessException ue)
                {
                    this.strErrorMessage = "Skipping Directory, " + ue.Message; 
                }
                catch (Exception ex)
                {
                    this.strErrorMessage = ex.Message;
                }
            }
        }

        #region Private Methods
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        public static void ListDefaultFileTypes()
        {
            Console.WriteLine("Default File Types Located by Scanner: ");

            Array FileTypes = Scanner.defaultFileTypes.ToArray();
            foreach (string fileType in FileTypes) 
            {
                Console.WriteLine(fileType);
            }
        }
    }
}
