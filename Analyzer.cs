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
        public bool continueRunning = false;                // Set to F by DiscoverMain when analyzer is done
        //private static SemaphoreSlim semaphore = new SemaphoreSlim(3);             // Used to control the number of queue items being processed at once
        private string _strCurrentAnalyzerFilePath;             // Holds the file being analyzed
        private uint _intAnalyzerFileCount;                    // Holds the number of files that have been analyzed

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
        #endregion

        public Analyzer(ConcurrentQueue<DiscoveredItem> cq)
        {
            this.cq = cq;
            this.continueRunning = false;
        }

        public void Go(byte numThreadsIn)
        {
            this.continueRunning = true;

            // While still 'live'
            while (this.continueRunning || !this.cq.IsEmpty)
            {
                // While the 'continueRunning' flag is true (it will be set to F by DiscoverMain when scanner is complete)
                // Pull entries from the queue and analyze them

                // Works
                List<Action> actionList = new List<Action>();

                //Action action = () =>
                //{
                //    this.ProcessQueueEntry();
                //};
                for (int i = 1; i <= numThreadsIn; i++)
                {
                    actionList.Add(new Action(() => this.ProcessQueueEntry()));
                }

                //Parallel.Invoke(action, action, action);
                Parallel.Invoke(actionList.ToArray());
            }
        }

        //public async Task Go()
        //{
        //    this.continueRunning = true;
        //    //semaphore = new SemaphoreSlim(3);

        //    // While still 'live'
        //    while (this.continueRunning || !this.cq.IsEmpty)
        //    {
        //        // While the 'continueRunning' flag is true (it will be set to F by DiscoverMain when scanner is complete)
        //        // Pull entries from the queue and analyze them
        //        //await this.ProcessQueueEntry();

        //        // Works
        //        Action action = () =>
        //        {
        //            this.ProcessQueueEntry(true);
        //        };
        //        Parallel.Invoke(action, action, action);


        //        //Action action = (async delegate
        //        //{
        //        //    await this.ProcessQueueEntry();
        //        //});
        //        //Parallel.Invoke(action, action, action);


        //        //Action action = () =>
        //        //{
        //        //    int localSum = 0;
        //        //    int localValue;
        //        //    while (cq.TryDequeue(out localValue)) localSum += localValue;
        //        //    Interlocked.Add(ref outerSum, localSum);
        //        // handle queue item or wait 1 second if not queue items left before querying again?
        //        //};

        //        //// Start 4 concurrent consuming actions.
        //        //Parallel.Invoke(action, action, action, action);


        //        //// No work - single?
        //        //semaphore.Wait();
        //        ////await Task.Run(async delegate { await this.ProcessQueueEntry(); });
        //        //await this.ProcessQueueEntry();
        //        //semaphore.Release();


        //        // No work - single?
        //        //await semaphore.WaitAsync();
        //        ////await Task.Run(async delegate { await this.ProcessQueueEntry(); });
        //        //await this.ProcessQueueEntry();
        //        //semaphore.Release();




        //        //await semaphore.WaitAsync();
        //        ////await Task.Run(async delegate { await this.ProcessQueueEntry(); });
        //        //_ = Task.Run(() => {

        //        //    this.ProcessQueueEntry(true); 


        //        //});
        //        //semaphore.Release();

        //        //Task.Run(async delegate { this.ProcessQueueEntry(); });
        //        //                Task.Run(async delegate {
        //        //                  await


        //        ////semaphore.Wait();
        //        //await Task.Run(async delegate  
        //        //{
        //        //    //    if (this.objStash != null && !this.objStash.StartSync(out string errMsg))
        //        //    //    {
        //        //    //        System.Windows.MessageBox.Show("An Error Occurred Starting Sync - " + errMsg, "Unable to Start Sync", MessageBoxButton.OK, MessageBoxImage.Error);
        //        //    //        this.cmdStop.Visibility = Visibility.Hidden;
        //        //    //        this.cmdStart.Visibility = Visibility.Visible;
        //        //    //    }
        //        //    await semaphore.WaitAsync();
        //        //    Console.WriteLine(DateTime.Now + " Starting Queue " + semaphore.CurrentCount);
        //        //    this.ProcessQueueEntry();
        //        //    //await Task.Delay(2000);
        //        //    Console.WriteLine(DateTime.Now + " Ending Queue " + semaphore.CurrentCount);
        //        //    semaphore.Release();
        //        //});
        //        ////semaphore.Release();
        //        ///

        //    }
        //}

        private void ProcessQueueEntry()
        {
            // Get item from queue and analyze it
            if (this.cq.TryDequeue(out DiscoveredItem diEntry))
            {
                Console.WriteLine(DateTime.Now + " Processing Entry " + diEntry.fileName);
                this.intAnalyzerFileCount++;
                this.strCurrentAnalyzerFilePath = diEntry.filePath;
                this.AnalyzeEntry(diEntry);
            }
            else
            {
                // if no task in the queue, wait 3 seconds for queue to maybe fill up again before releasing the thread
                Console.WriteLine(DateTime.Now + " Waiting...");
                Thread.Sleep(3000);
            }
        }

        private void AnalyzeEntry(DiscoveredItem diIn)
        {
            // Analyze the entry
            diIn.fileAnalysis = "Done";

            // Simulate work - ToDo Remove
            Thread.Sleep(3000);
        }

        //private async Task ProcessQueueEntry()
        //{
        //    // Get item from queue and analyze it
        //    if (this.cq.TryDequeue(out DiscoveredItem diEntry))
        //    {
        //        Console.WriteLine(DateTime.Now + " Processing Entry " + diEntry.fileName);
        //        this.intAnalyzerFileCount++;
        //        this.strCurrentAnalyzerFilePath = diEntry.filePath;
        //        await this.AnalyzeEntry(diEntry);
        //    }
        //    else
        //    {
        //        // if no task in the queue, wait 1 second before releasing the thread
        //        Console.WriteLine(DateTime.Now + " Waiting...");
        //        await Task.Delay(5000);
        //    }
        //}

        //private async Task AnalyzeEntry(DiscoveredItem diIn)
        //{
        //    // Analyze the entry
        //    diIn.fileAnalysis = "Done";

        //    //ToDo Remove
        //    await Task.Delay(5000);
        //}

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
