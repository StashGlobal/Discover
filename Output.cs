using System;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;                // For computed properties and notifications
using System.Text.Json;                     // For outputting DiscoveredItem objects to the output file
using System.Text.Json.Serialization;       // For outputting DiscoveredItem objects to the output file

namespace Stash.Discover
{
    // Manages JSON output files
    class Output
    {
        private const uint NUM_OUTPUT_RECORDS = 100;     // The minimum number of records in queue required before dumping to output file
        private string strLastFileName = "";            // Tracks the last scanned file name
        private uint intLastFileCount = 0;              // Tracks the last known number of scanned files
        private string strLastAnalyzerFileName = "";    // Tracks the last analyzed file name
        private uint intLastAnalyzerFileCount = 0;      // Tracks the last known number of analyzed files
        private ConcurrentQueue<DiscoveredItem> outputcq = null;        // Pointer to the global output queue setup in DiscoverMain
        private int ScannerRow = 0;                         // Tracks the console row where scanner output should be written
        private int AnalyzerRow = 0;                        // Tracks the console row where analyzer output should be written
        
        public bool continueRunning = false;                // Set to F by DiscoverMain when analyzer is done

        private int counter = 0;
        private string strStatus = "";
        private char charSpin = '|';
        private bool firstTime = true;
        private bool lastTime = false;

        // Constructor        
        public Output(ConcurrentQueue<DiscoveredItem> cq)
        {
            this.outputcq = cq;
        }

        // Updates the console display with the current scan and analyzed filenames / counts
        public void UpdateDisplay(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                if (e == null || e.PropertyName == "") { return; }
                if (e.PropertyName == "strCurrentFilePath")
                {
                    Console.WriteLine(". - File Count: " + ((Scanner)sender).intFileCount + " - File: " + ((Scanner)sender).strCurrentFilePath);
                }
                if (e.PropertyName == "strCurrentAnalyzerFilePath")
                {
                    Console.WriteLine(". - Analyzed File Count: " + ((Analyzer)sender).intAnalyzerFileCount + " - File: " + ((Analyzer)sender).strCurrentAnalyzerFilePath);
                }
            } catch (Exception)
            {
                // No action, just return, silently ignoring the error
            }
        }

        // Initializes the output file
        public void startOutputFile(string outputFile)
        {
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(outputFile, false))
            {
                sw.Write("[");      // Opening JSON array tag - the output will be an array of discovered items
            }    
        }

        // Updates the output file with entries from the output queue; requires minimum of NUM_OUTPUT_RECORDS in queue
        public async Task Go(string outputFile)
        {
            DiscoveredItem di = null;
            string strJson = "";
            string strOutput = "";

            this.continueRunning = true;

            this.startOutputFile(outputFile);       // Initialize the output file

            while (this.continueRunning)
            {
                if (this.outputcq.Count() >= Output.NUM_OUTPUT_RECORDS)
                {
                    for (int i = 0; i < Output.NUM_OUTPUT_RECORDS; i++)
                    {
                        if (this.outputcq.TryDequeue(out di))
                        {
                            strJson = System.Text.Json.JsonSerializer.Serialize(di, di.GetType());
                            strOutput += strJson;
                        }
                    }
                    // Open Output file for append, write strOutput, close file
                    using (System.IO.StreamWriter sw = new System.IO.StreamWriter(outputFile, true))
                    {
                        await sw.WriteAsync(strOutput);
                    }
                } else
                {
                    //Console.WriteLine(DateTime.Now + " Output - Waiting...");
                    await Task.Delay(3000);
                }
            }

            // Dump remaining records in the queue after this task was 'stopped'
            strJson = ""; strOutput = "";
            while (! this.outputcq.IsEmpty)
            {
                if (this.outputcq.TryDequeue(out di))
                {                    
                    strJson = JsonSerializer.Serialize(di, di.GetType());
                    strOutput += strJson;
                }
            }
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(outputFile, true))
            {
                await sw.WriteAsync(strOutput);
            }

        }

        // Wraps the output file with opening and closing JSON array tags and finishs any remaining items in the queue
        public void finishOutputFile(string outputFile)
        {
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(outputFile, true))
            {
                sw.Write("]");      // Closing JSON array tag - the output will be an array of discovered items
            }
        }

        // Prints the spinning cursor only
        public void Turn()
        {
            counter++;
            switch (counter % 4)
            {
                case 0: charSpin = '/'; counter = 0; break;
                case 1: charSpin = '-'; break;
                case 2: charSpin = '\\'; break;
                case 3: charSpin = '|'; break;
            }

            Console.Write('\r' + charSpin);
        }

        // Prints the spinning cursor with message
        public void Turn(string prefix, string suffix, string firstMessage, string lastMessage)
        {
            counter++;
            switch (counter % 4)
            {
                case 0: charSpin = '/'; counter = 0; break;
                case 1: charSpin = '-'; break;
                case 2: charSpin = '\\'; break;
                case 3: charSpin = '|'; break;
            }

            if (firstTime)
            {
                firstTime = false;
            }

            int oldLength = strStatus.Length;
            strStatus = prefix + charSpin + suffix;
            Console.Write(new string('\b', oldLength) + strStatus);
        }
    }
}
