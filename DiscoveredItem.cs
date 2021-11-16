using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stash.Discover
{
    class DiscoveredItem
    {
        public string fileName;
        public long fileSize;
        public DateTime fileDTS;
        public string fileAnalysis;
        public string fileMimeType;
        public string filePath;

        public DiscoveredItem()
        {
            fileName = "";
            fileSize = 0;
            fileDTS = new DateTime(0);
            fileAnalysis = "";
            fileMimeType = "Unknown";
            filePath = "";
        }

        public DiscoveredItem(string filePathIn)
        {
            fileName = "";
            fileSize = 0;
            fileDTS = new DateTime(0);
            fileAnalysis = "";
            fileMimeType = "Unknown";
            filePath = filePathIn;

            if (File.Exists(filePathIn))
            {
                FileInfo fi = new FileInfo(filePathIn);
                fileName = fi.Name;
                fileSize = fi.Length;
                fileDTS = fi.LastWriteTimeUtc;
            }
        }
    }
}
