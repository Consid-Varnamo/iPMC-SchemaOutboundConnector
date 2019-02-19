using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace inRiver.Connectors.Schema.Output.Helpers
{
    /// <summary>
    /// Different storage mediums supported
    /// </summary>
    public enum AzureStorageMedium
    {
        Blob,       
        File        // can't be emulated locally as of storage emulator 
    }
}
