using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using inRiver.Remoting.Extension;


namespace inRiver.Connectors.Schema.Output.Helpers
{
    internal interface IExtensionHelper
    {
        inRiverContext Context { get; set; }
    }
}
