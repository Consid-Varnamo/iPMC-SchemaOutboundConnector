using inRiver.Remoting.Extension;


namespace inRiver.Connectors.Schema.Output.Helpers
{
    internal class ExtensionHelper : IExtensionHelper
    {
        protected ExtensionHelper(inRiverContext context)
        {
            this.Context = context;
        }

        protected ExtensionHelper(IExtensionHelper helper)
        {
            this.Context = helper.Context;
        }

        public inRiverContext Context { get; set; }

    }
}
