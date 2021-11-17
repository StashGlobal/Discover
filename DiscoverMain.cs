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
        private bool listFileTypes = false;         // T to dump the default file types and exit
        private string fileTypes = "";              // A comma-separated list of file types to identify and analyze, defaults are set in Scanner.cs
        private string basePath = "";               // The directory to start the scanner from, defaults to root of file system (all drives on Windows or '/' on Linux/MacOSX)
        private string excludePaths = "";            // A comma-separate list of directories to exclude
        private byte numThreads = 3;                // Number of threads to use for the analysis by Analyzer
        private string outputFile = "." + Path.DirectorySeparatorChar + "discover.json";
        private int returnCode = 0;                 // The return code to set when program exits
        private Scanner scanner = null;             // The scanner object
        private Output output = null;               // The output object
        private Analyzer analyzer = null;           // The analyzer object
        private ConcurrentQueue<DiscoveredItem> cq = null;  // A global thread-safe queue to store the files for analysis
        private ConcurrentQueue<DiscoveredItem> outputCQ = null;    // A global thread-safe queue to store output information
        private Task[] taskScanner = null;
        private Task[] taskAnalyzer = null;
        private Task[] taskOutput = null;

        public class Options
        {
            [Option('p', "path", Required = false, HelpText = "Base path to start scanner in (default root of the file system)")]
            public string BasePath { get; set; }

            [Option('e', "exclude path", Required = false, HelpText = "Comma-separated list of directories to exclude from the scanner (default none)")]
            public string ExcludePaths { get; set; }

            [Option('t', "types", Required = false, HelpText = "Comma-separated list of MIME types the Scanner should identify and analyze")]
            public string FileTypes { get; set; }

            [Option('l', "list", Required = false, HelpText = "List default MIME types the Scanner will locate and identify")]
            public bool ListFileTypes { get; set; }

            [Option('n', "threads", Required = false, HelpText = "Number of threads to use for analysis (default 3)")]
            public byte NumThreads { get; set; }

            [Option('o', "output", Required = false, HelpText = "Name and path to output file (default discover json)")]
            public string OutputFile { get; set; }

            [Option('v', "verbose", Required = false, HelpText = "See verbose output messages.")]
            public bool Verbose { get; set; }
        }

        private void ArgCheck()
        {
            if (this.numThreads <= 0 || this.numThreads > 20)
            {
                throw new ArgumentOutOfRangeException("Threads", "Invalid Number of Threads (must be 1-20)");
            }
            if (this.outputFile == "")
            {
                throw new ArgumentOutOfRangeException("output", "Output File Cannot be Blank");
            }
            
            // Check output file directory exists
            System.IO.FileInfo fi = new System.IO.FileInfo(this.outputFile);
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(fi.DirectoryName);
            if (fi.DirectoryName != "" && ! di.Exists)
            {
                throw new ArgumentOutOfRangeException("output", "Ouput Directory Does Not Exist");
            }

            // Check if file exists, and prompt for overwrite
            if (System.IO.File.Exists(this.outputFile))
            {
                Console.WriteLine("Output File Exists - Overwrite? (y/n)");
                ConsoleKeyInfo cKI = Console.ReadKey();
                if (cKI.Key != ConsoleKey.Y)
                {
                    Environment.Exit(-1);
                }
            }
        }

        public DiscoverMain()
        {
            this.cq = new ConcurrentQueue<DiscoveredItem>();
            this.outputCQ = new ConcurrentQueue<DiscoveredItem>();
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
                       if (o.ListFileTypes) { cliProgram.listFileTypes = true; }
                       if (o.FileTypes != null && o.FileTypes != "") { cliProgram.fileTypes = o.FileTypes; }
                       if (o.BasePath != null && o.BasePath != "") { cliProgram.basePath = o.BasePath; }
                       if (o.ExcludePaths != null && o.ExcludePaths != "") { cliProgram.excludePaths = o.ExcludePaths; }
                       if (o.NumThreads > 0) { cliProgram.numThreads = o.NumThreads; }
                   })
                   .WithNotParsed<Options>((errs) => HandleParseErrors(errs));

                if (cliProgram.listFileTypes)
                {
                    Scanner.ListDefaultFileTypes();
                    Environment.Exit(0);
                }
                // Sanity check the arguments
                cliProgram.ArgCheck();

                // Create the scanner and output objects that depend on the command line options
                cliProgram.scanner = new Scanner(cliProgram.cq, cliProgram.fileTypes, cliProgram.excludePaths);
                cliProgram.analyzer = new Analyzer(cliProgram.cq, cliProgram.outputCQ);
                cliProgram.output = new Output(cliProgram.outputCQ);

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

                cliProgram.taskAnalyzer = new Task[] {
                Task.Run(delegate
                    {
                        cliProgram.analyzer.Go(cliProgram.numThreads);
                    })
                };

                cliProgram.taskOutput = new Task[]
                {
                Task.Run(async delegate
                    {
                        await cliProgram.output.Go(cliProgram.outputFile);
                    })
                };

                // Wait for scanner task to finish
                Task.WaitAll(cliProgram.taskScanner);

                cliProgram.analyzer.continueRunning = false;

                // Wait for analyzer task to finish
                Task.WaitAll(cliProgram.taskAnalyzer);

                cliProgram.output.continueRunning = false;

                // Wait for the output task to finish
                Task.WaitAll(cliProgram.taskOutput);

                cliProgram.output.finishOutputFile(cliProgram.outputFile);

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
