using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;                // For computed properties and notifications

namespace Stash.Discover
{
    // Manages JSON output files
    class Output
    {
        private string strLastFileName = "";
        private uint intLastFileCount = 0;

        // Placeholder
        public Output()
        {

        }

        public void UpdateDisplay(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                if (e == null || e.PropertyName == "") { return; }
                if (e.PropertyName != "intFileCount")
                {
                    Console.WriteLine(". - File Count: " + ((Scanner)sender).intFileCount + " - File: " + ((Scanner)sender).strCurrentFilePath);
                }
            } catch (Exception)
            {
                // No action, just return, silently ignoring the error
            }
        }
    }
}
