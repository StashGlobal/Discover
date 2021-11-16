using System;
using CommandLine;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Text;

// Build Requirements for VS 2019 using Publishing Wizard
// - Create a publish target for Windows, Linux, MacOS
// - From Build menu, select "Publish", then select appropriate target

namespace Stash.Discover
{   
    // Main program entry, command line parsing, and output control
    // Return Codes:
    //  -1 - Command line options parse error
    //  0 - Success
    // 
    class DiscoverMain 
    {
        private bool verbosity = false;             // T to enable more output
        private string fileTypes = "";              // A comma-separated list of file types to identify and analyze, defaults are set in Scanner.cs
        private string basePath = "";               // The directory to start the scanner from, defaults to root of file system (all drives on Windows or '/' on Linux/MacOSX)
        private string excludePaths = "";            // A comma-separate list of directories to exclude
        private byte numThreads = 3;                // Number of threads to use for the analysis by Analyzer
        private int returnCode = 0;                 // The return code to set when program exits
        private Scanner scanner = null;             // The scanner object
        private Output output = null;               // The output object
        private Analyzer analyzer = null;           // The analyzer object
        private ConcurrentQueue<DiscoveredItem> cq = null;  // A global thread-safe queue to store the files for analysis
        //private List<Task> runningTasks = new List<Task>();
        private Task[] taskScanner = null;
        private Task[] taskAnalyzer = null;
        
        public class Options
        {
            [Option('p', "path", Required = false, HelpText = "Base path to start scanner in (default root of the file system)")]
            public string BasePath { get; set; }

            [Option('e', "exclude path", Required = false, HelpText = "Comma-separated list of directories to exclude from the scanner (default none)")]
            public string ExcludePaths { get; set; }

            [Option('t', "types", Required = false, HelpText = "Comma-separated list of MIME types the Scanner should identify and analyze")]
            public string FileTypes { get; set; }

            [Option('n', "threads", Required = false, HelpText = "Number of threads to use for analysis (default 3)")]
            public byte NumThreads { get; set; }
            
            [Option('v', "verbose", Required = false, HelpText = "See verbose output messages.")]
            public bool Verbose { get; set; }
        }

        private void ArgCheck()
        {
            if (this.numThreads <= 0 || this.numThreads > 20)
            {
                throw new ArgumentOutOfRangeException("Threads", "Invalid Number of Threads (must be 1-20)");
            }

        }

        public DiscoverMain()
        {
            this.cq = new ConcurrentQueue<DiscoveredItem>();
        }

        static void Main(string[] args)
        {
            DiscoverMain cliProgram = new DiscoverMain();

            try
            {
                // Parse first portion of command line options 
                Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       if (o.Verbose) { cliProgram.verbosity = true; }
                       if (o.FileTypes != null && o.FileTypes != "") { cliProgram.fileTypes = o.FileTypes; }
                       if (o.BasePath != null && o.BasePath != "") { cliProgram.basePath = o.BasePath; }
                       if (o.ExcludePaths != null && o.ExcludePaths != "") { cliProgram.excludePaths = o.ExcludePaths; }
                       if (o.NumThreads > 0) { cliProgram.numThreads = o.NumThreads; }
                   })
                   .WithNotParsed<Options>((errs) => HandleParseErrors(errs));

                // Sanity check the arguments
                cliProgram.ArgCheck();

                // Create the scanner and output objects that depend on the command line options
                cliProgram.scanner = new Scanner(cliProgram.cq, cliProgram.fileTypes, cliProgram.excludePaths);
                cliProgram.analyzer = new Analyzer(cliProgram.cq);
                cliProgram.output = new Output();

                // Set the property notification handlers
                cliProgram.scanner.PropertyChanged += cliProgram.output.UpdateDisplay;      // Connect output routines to scanner
                cliProgram.analyzer.PropertyChanged += cliProgram.output.UpdateDisplay;   // Connect output routines to analyzer
                
                // Kick off scanner and analyzer threads - then wait until scanner and analyzer report 'done'
                cliProgram.taskScanner = new Task[] {
                Task.Run(async delegate
                    {
                        await cliProgram.scanner.Go(cliProgram.basePath);
                    })
                };

                // Works
                //cliProgram.taskAnalyzer = new Task[] {
                //Task.Run(async delegate
                //    {
                //        await cliProgram.analyzer.Go();
                //    })
                //};

                cliProgram.taskAnalyzer = new Task[] {
                Task.Run(delegate
                    {
                        cliProgram.analyzer.Go(cliProgram.numThreads);
                    })
                };

                //cliProgram.taskScanner.Append<Task>(Task.Run(async delegate
                //{
                //    await cliProgram.scanner.Go(cliProgram.basePath);
                //}));

                //cliProgram.runningTasks.Add(
                //    Task.Run(async delegate
                //    {
                //        await cliProgram.scanner.Go(cliProgram.basePath);
                //    }));

                // how to have analyzer handle queue AND wait until scanner is done before exiting...
                // Task waitall, then kill parallel invoke when done?

                //int outerSum = 0;
                //// An action to consume the ConcurrentQueue.
                //Action action = () =>
                //{
                //    int localSum = 0;
                //    int localValue;
                //    while (cq.TryDequeue(out localValue)) localSum += localValue;
                //    Interlocked.Add(ref outerSum, localSum);
                // handle queue item or wait 1 second if not queue items left before querying again?
                //};

                //// Start 4 concurrent consuming actions.
                //Parallel.Invoke(action, action, action, action);



                //Task.WaitAll(cliProgram.runningTasks.ToArray());

                // Wait for scanner task to finish
                Task.WaitAll(cliProgram.taskScanner);

                cliProgram.analyzer.continueRunning = false;

                // Wait for analyzer task to finish
                Task.WaitAll(cliProgram.taskAnalyzer);


                //this.objStash.runningTasks.Add(
                //    Task.Run(async delegate
                //    {
                //        await this.objStash.EventWorker.HandleEventsErrored(this.objStash.EventWorker.cts.Token);
                //    });



                //Task.Run(async delegate
                //{
                //    //await this.objStash.EventWorker.HandleEvents(this.objStash.EventWorker.cts.Token);
                //    await BackupEvent.HandleRenameNotFound(objStash, this.EventPath, this.EventPathDest);

                //});



                //    this.objStash.runningTasks.Add(
                //    Task.Run(async delegate
                //    {
                //        await this.objStash.EventWorker.HandleEventsErrored(this.objStash.EventWorker.cts.Token);
                //    })

                //);



                //await Task.Run(() => {
                //    if (this.objStash != null && !this.objStash.StartSync(out string errMsg))
                //    {
                //        System.Windows.MessageBox.Show("An Error Occurred Starting Sync - " + errMsg, "Unable to Start Sync", MessageBoxButton.OK, MessageBoxImage.Error);
                //        this.cmdStop.Visibility = Visibility.Hidden;
                //        this.cmdStart.Visibility = Visibility.Visible;
                //    }
                //});



                //int outerSum = 0;
                //// An action to consume the ConcurrentQueue.
                //Action action = () =>
                //{
                //    int localSum = 0;
                //    int localValue;
                //    while (cq.TryDequeue(out localValue)) localSum += localValue;
                //    Interlocked.Add(ref outerSum, localSum);
                //};

                //// Start 4 concurrent consuming actions.
                //Parallel.Invoke(action, action, action, action);

                cliProgram.returnCode = 0;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Console.WriteLine(ex.Message);
                cliProgram.returnCode = -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("An Error Occurred: " + ex.Message);
                if (cliProgram.verbosity)
                {
                    Console.WriteLine("Please report the following information to STASH for assistance\n\r" + ex.StackTrace);
                }
            }
            finally
            {                
                Environment.Exit(cliProgram.returnCode);
            }
        }

        #region Public Methods
        // Handles any parse errors returne from the command line options parser
        public static void HandleParseErrors(IEnumerable<Error> errs)
        {
            Environment.Exit(-1);
        }
        #endregion
    }
}
