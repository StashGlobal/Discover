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
        public string fileName { get; set; }
        public long fileSize { get; set; }
        public DateTime fileDTS { get; set; }
        public string fileAnalysis { get; set; }
        public string fileMimeType { get; set; }
        public string filePath { get; set; }

        public override string ToString()
        {
            return "Discovered Item, File: " + this.filePath + " MIME: " + this.fileMimeType + " Size: " + this.fileSize + " Analysis: " + this.fileAnalysis;
        }

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
