using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;                // For computed properties and notifications
using System.Runtime.CompilerServices;      // For computed properties and notifications

namespace Stash.Discover
{
    // Analyzes the content found by DiscoverMain
    class Analyzer : INotifyPropertyChanged
    {
        private ConcurrentQueue<DiscoveredItem> cq = null;
        private ConcurrentQueue<DiscoveredItem> outputcq = null;
        public bool continueRunning = false;                // Set to F by DiscoverMain when scanner is done
        private string _strCurrentAnalyzerFilePath;             // Holds the file being analyzed
        private uint _intAnalyzerFileCount;                    // Holds the number of files that have been analyzed
        private string _strAnalyzerErrorMessage = "";               // Tracks non-fatal error messages for printing to screen

        #region Public Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Computed Properties
        // When the analyzer updates these properties, it will notify DiscoverMain, which then updates the display
        // These properties are monitored by Output.cs:UpdateDisplay() - the property change handlers are set in DiscoverMain.cs:Main()

        public string strCurrentAnalyzerFilePath
        {
            get { return this._strCurrentAnalyzerFilePath; }
            set
            {
                if (this._strCurrentAnalyzerFilePath != value)
                {
                    this._strCurrentAnalyzerFilePath = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public uint intAnalyzerFileCount
        {
            get { return this._intAnalyzerFileCount; }
            set
            {
                if (this._intAnalyzerFileCount != value)
                {
                    this._intAnalyzerFileCount = value;
                    NotifyPropertyChanged();
                }
            }
        }

        // Used to track error or warning messages that are not fatal
        public string strAnalyzerErrorMessage
        {
            get { return this._strAnalyzerErrorMessage; }
            set
            {
                if (this._strAnalyzerErrorMessage != value)
                {
                    this._strAnalyzerErrorMessage = value;
                    NotifyPropertyChanged();
                }
            }
        }
        #endregion

        public Analyzer(ConcurrentQueue<DiscoveredItem> cq, ConcurrentQueue<DiscoveredItem> outputcq)
        {
            this.cq = cq;
            this.outputcq = outputcq;
            this.continueRunning = false;
        }

        public void Go(byte numThreadsIn)
        {
            this.continueRunning = true;

            while (this.continueRunning || !this.cq.IsEmpty)
            {
                // While the 'continueRunning' flag is true (it will be set to F by DiscoverMain when scanner is complete)
                // Pull entries from the queue and analyze them
                List<Action> actionList = new List<Action>();
                for (int i = 1; i <= numThreadsIn; i++)
                {
                    actionList.Add(new Action(() => this.ProcessQueueEntry()));
                }

                Parallel.Invoke(actionList.ToArray());
            }
        }        

        private void ProcessQueueEntry()
        {
            // Get item from queue and analyze it
            if (this.cq.TryDequeue(out DiscoveredItem diEntry))
            {
                //Console.WriteLine(DateTime.Now + " Processing Entry " + diEntry.fileName);
                Monitor.Enter(this);    // Count is a shared properties - could be written by any of the threads running processqueueentry()
                this.intAnalyzerFileCount++;
                this.strCurrentAnalyzerFilePath = diEntry.filePath;
                Monitor.Exit(this);
                this.AnalyzeEntry(diEntry);         
                this.outputcq.Enqueue(diEntry);
            }
            else
            {
                // if no task in the queue, wait 1 second for queue to maybe fill up again before releasing the thread
                //Console.WriteLine(DateTime.Now + " Waiting...");                
                Thread.Sleep(1000);
            }
        }

        private void AnalyzeEntry(DiscoveredItem diIn)
        {
            // Analyze the entry
            diIn.fileAnalysis = "N/A";

            // Simulate work - ToDo Remove
            //Thread.Sleep(3000);
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
    }
}
