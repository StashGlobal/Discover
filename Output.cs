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
        //private string strLastFileName = "";            // Tracks the last scanned file name
        //private uint intLastFileCount = 0;              // Tracks the last known number of scanned files
        //private string strLastAnalyzerFileName = "";    // Tracks the last analyzed file name
        //private uint intLastAnalyzerFileCount = 0;      // Tracks the last known number of analyzed files
        private ConcurrentQueue<DiscoveredItem> outputcq = null;        // Pointer to the global output queue setup in DiscoverMain
        private int ScannerRow = 0;                         // Tracks the console row where scanner output should be written
        private int AnalyzerRow = 0;                        // Tracks the console row where analyzer output should be written
        private int ErrorRow = 0;                        // Tracks the console row where analyzer output should be written

        public bool continueRunning = false;                // Set to F by DiscoverMain when analyzer is done

        private int ScannerCounter = 0;                 // Used for tracking spinner character for Scanner
        private int AnalyzerCounter = 0;                // Used for tracking spinner character for Analyzer
        private byte ScannerSpinnerCounter = 0;         // Used for tracking how many files should be scanned before spinner is updated
        private string strStatus = "";
        private char charSpin = '|';
        private bool firstTime = true;
        //private bool lastTime = false;

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
                int IntConsoleWidth = Console.WindowWidth;
                int IntBuffer = 5;
                int IntCounterSize = 10;
                int IntTurnSize = 1;
                string StrMessage = "";
                Monitor.Enter(this);
                if (e.PropertyName == "strCurrentDirPath" || e.PropertyName == "intFileCount")
                {
                    // Update if the current scanned directory or the file count changes
                    // Move curser to start of scanner line and update
                    Console.CursorLeft = 1; Console.CursorTop = this.ScannerRow;
                    this.Turn(ref this.ScannerCounter);
                    string tDirPath = ((Scanner)sender).strCurrentDirPath;
                    string tHeader = " | Scanner  | ";
                    string tCounter = "Matched Count: ";
                    string tDir = " Dir: ";
                    string tDirSeparator = "...";
                    int availMessageLength = IntConsoleWidth - IntBuffer - IntTurnSize - tHeader.Length - tCounter.Length - IntCounterSize - tDir.Length;
                    StrMessage = tHeader + tCounter + ((Scanner)sender).intFileCount + tDir;
                    if (tDirPath.Length > availMessageLength)
                    {
                        // Get some substring of tDirPath that fits in availMessageLength (e.g. 30% of first, 70% of last)?
                        int IntStartLength = (int)Math.Floor((double)(availMessageLength * .3));
                        int IntEndLength = availMessageLength - IntStartLength - tDirSeparator.Length;
                        StrMessage += tDirPath.Substring(0, IntStartLength) + "..." + tDirPath.Substring(tDirPath.Length - IntEndLength);
                    }
                    else
                    {
                        StrMessage += tDirPath;
                    }
                    Console.Write(StrMessage);
                    Console.Write(new string(' ', IntConsoleWidth - StrMessage.Length - 1));
                }
                if (e.PropertyName == "strCurrentFilePath")
                {
                    // Update the spinner only
                    // Move curser to start of scanner line and update
                    Console.CursorLeft = 1; Console.CursorTop = this.ScannerRow;
                    this.ScannerSpinnerCounter++;
                    if (this.ScannerSpinnerCounter % 100 == 0)
                    {
                        this.Turn(ref this.ScannerCounter);
                        this.ScannerSpinnerCounter = 0;
                    }
                }
                if (e.PropertyName == "strCurrentAnalyzerFilePath")
                {
                    Console.CursorLeft = 1; Console.CursorTop = this.AnalyzerRow;
                    this.Turn(ref this.AnalyzerCounter);
                    string tFilePath = ((Analyzer)sender).strCurrentAnalyzerFilePath;
                    string tHeader = " | Analyzer | ";
                    string tCounter = "Count: ";
                    string tFile = " File: ";
                    string tFileSeparator = "...";
                    int availMessageLength = IntConsoleWidth - IntBuffer - IntTurnSize - tHeader.Length - tCounter.Length - IntCounterSize - tFile.Length;
                    StrMessage = tHeader + tCounter + ((Analyzer)sender).intAnalyzerFileCount + tFile;
                    if (tFilePath.Length > availMessageLength)
                    {
                        // Get some substring of tDirPath that fits in availMessageLength (e.g. 30% of first, 70% of last)?
                        int IntStartLength = (int)Math.Floor((double)(availMessageLength * .3));
                        int IntEndLength = availMessageLength - IntStartLength - tFileSeparator.Length;
                        StrMessage += tFilePath.Substring(0, IntStartLength) + "..." + tFilePath.Substring(tFilePath.Length - IntEndLength);
                    }
                    else
                    {
                        StrMessage += tFilePath;
                    }
                    Console.Write(StrMessage);
                    Console.Write(new string(' ', IntConsoleWidth - StrMessage.Length - 1));
                }
                if (e.PropertyName == "strErrorMessage")
                {
                    Console.CursorLeft = 1; Console.CursorTop = this.ErrorRow;
                    string tErrMsg = ((Scanner)sender).strErrorMessage;
                    string tHeader = "  | Message  | ";
                    string tMessageSeparator = "...";
                    int availMessageLength = IntConsoleWidth - IntBuffer - tHeader.Length;
                    StrMessage = tHeader;
                    if (tErrMsg.Length > availMessageLength)
                    {
                        // Get some substring of tDirPath that fits in availMessageLength (e.g. 30% of first, 70% of last)?
                        int IntStartLength = (int)Math.Floor((double)(availMessageLength * .3));
                        int IntEndLength = availMessageLength - IntStartLength - tMessageSeparator.Length;
                        StrMessage += tErrMsg.Substring(0, IntStartLength) + "..." + tErrMsg.Substring(tErrMsg.Length - IntEndLength);
                    }
                    else
                    {
                        StrMessage += tErrMsg;
                    }
                    Console.Write(StrMessage);
                    Console.Write(new string(' ', IntConsoleWidth - StrMessage.Length - 1));
                }
                if (e.PropertyName == "strAnalyzerErrorMessage")
                {
                    Console.CursorLeft = 1; Console.CursorTop = this.ErrorRow;
                    string tErrMsg = ((Analyzer)sender).strAnalyzerErrorMessage;
                    string tHeader = "  | Message | ";
                    string tMessageSeparator = "...";
                    int availMessageLength = IntConsoleWidth - IntBuffer - tHeader.Length;
                    StrMessage = tHeader;
                    if (tErrMsg.Length > availMessageLength)
                    {
                        // Get some substring of tDirPath that fits in availMessageLength (e.g. 30% of first, 70% of last)?
                        int IntStartLength = (int)Math.Floor((double)(availMessageLength * .3));
                        int IntEndLength = availMessageLength - IntStartLength - tMessageSeparator.Length;
                        StrMessage += tErrMsg.Substring(0, IntStartLength) + "..." + tErrMsg.Substring(tErrMsg.Length - IntEndLength);
                    }
                    else
                    {
                        StrMessage += tErrMsg;
                    }
                    Console.Write(StrMessage);
                    Console.Write(new string(' ', IntConsoleWidth - StrMessage.Length - 1));
                }
            }
            catch (Exception)
            {
                // No action, just return, silently ignoring the error
            }
            finally
            {
                Monitor.Exit(this);
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
            while (!this.outputcq.IsEmpty)
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

        // Sets the origin points for the output lines
        public void setConsoleOrigins()
        {
            this.ScannerRow = Console.CursorTop;            // Scanner output first
            this.AnalyzerRow = Console.CursorTop + 1;       // Line below scanner output for analyzer output
            this.ErrorRow = Console.CursorTop + 2;
        }

        public int getConsoleNextLine()
        {
            return this.ErrorRow + 1;
        }
        
        // Prints the spinning cursor only
        public void Turn(ref int counter)
        {
            counter++;
            switch (counter % 4)
            {
                case 0: charSpin = '/'; counter = 0; break;
                case 1: charSpin = '-'; break;
                case 2: charSpin = '\\'; break;
                case 3: charSpin = '|'; break;
            }

            Console.Write(charSpin.ToString());
        }

        // Prints the spinning cursor with message
        public void Turn(ref int counter, string prefix, string suffix, string firstMessage, string lastMessage)
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
