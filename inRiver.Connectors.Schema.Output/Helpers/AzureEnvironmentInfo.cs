using inRiver.Remoting.Extension;

namespace inRiver.Connectors.Schema.Output.Helpers
{
    /// <summary>
    /// Encapsxulates Azure Storage Parameters
    /// </summary>
    public class AzureEnvironmentInfo
    {
        /// <summary>
        /// Azure Storage Connection String
        /// </summary>
        public string AzureStorageConnection;
        /// <summary>
        /// End point URL for storage access
        /// </summary>
        public string AzureEndpointUrl;
        /// <summary>
        /// Container for Blobs, FileShare for files
        /// </summary>
        public string AzureContainer;

        public AzureEnvironmentInfo(inRiverContext context)
        {
            this.AzureStorageConnection = context?.Settings?[ConnectorSettings.AzureStorageConnection];
            this.AzureEndpointUrl = context?.Settings?[ConnectorSettings.AzureEndpointUrl];
            this.AzureContainer = context?.Settings?[ConnectorSettings.AzureContainer];
        }
    }
}
