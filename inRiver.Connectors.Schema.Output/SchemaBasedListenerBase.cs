using System.Collections.Generic;
using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Connectors.Schema.Output.Helpers;
using inRiver.SchemaBased;



namespace inRiver.Connectors.Schema.Output
{

    /// <summary>
    /// Contains base functionality to be shared by Listeners
    /// </summary>
    public abstract class SchemaBasedListenerBase
    {

        Dictionary<string, string> defaultSettings = null;

        /// <summary>
        /// If a derived class has additional settings to validate, this is how they will provide them to the base 
        /// </summary>
        /// <returns>List of setting values to insure exist</returns>
        protected abstract List<string> additionalSettingsToValidate();

        /// <summary>
        /// Settings from context
        /// </summary>
        protected Dictionary<string, string> adapterSettings { get; set; }

        /// <summary>
        /// Helper calss for Azure operations
        /// </summary>
        internal AzureListenerHelper azureListenerHelper { get; set; }

        /// <summary>
        /// Helper class for channel operations
        /// </summary>
        internal ChannelHelper channelHelper { get; set; }

        /// <summary>
        /// Helper class for common functions
        /// </summary>
        internal CommonFunctions common { get; set; }

        public inRiverContext Context { get; set; }

        public Dictionary<string, string> DefaultSettings
        {
            get
            {
                return getDefaultSettings();
            }
            set
            {
                defaultSettings = value;
            }
        }

        internal List<int> entittyIdsAlreadyWrittenToFile { get; private set; } = new List<int>();

        /// <summary>
        /// Set to true if context, etc. is set, useful for testing where context is set up manually
        /// </summary>
        protected bool? isValid { get; set; }

        /// <summary>
        /// Helper for XML schema mapping
        /// </summary>
        internal MappingHelper mappingHelper { get; set; }

        /// <summary>
        /// Business logic for Schema Based output
        /// </summary>
        internal SchemaBasedOutput schemaBasedOutput { get; set; }

        protected Dictionary<string, string> getDefaultSettings()
        {
            string extensionId = this.GetType().Name;
            var result = new Dictionary<string, string>();
            result[ConnectorSettings.AzureStorageConnection] = "DefaultEndpointsProtocol=https;AccountName=mystorageaccount;AccountKey=mystorageaccountkey;EndpointSuffix=core.windows.net";
            result[ConnectorSettings.AzureContainer] = "schemabased";
            result[ConnectorSettings.AzureEndpointUrl] = "https://mystorageaccount.file.core.windows.net/schemabased";
            result[ConnectorSettings.ChannelId] = "52906";
            result[ConnectorSettings.PublishFolder] = $@"Publish/{extensionId}";
            result[ConnectorSettings.ResourceFolder] = $@"Publish/{extensionId}/Resources";
            result[ConnectorSettings.CvlFolder] = $@"Publish/{extensionId}/CVLs";
            result[ConnectorSettings.PublishAsSingleFile] = "True";
            result[ConnectorSettings.MaxEntitiesInPublishedFile] = "0";
            result[ConnectorSettings.UpdateParentWhenAddEntity] = "True";
            if (Context?.ExtensionId == null)
            {
                result[ConnectorSettings.MappingXml] = ConnectorSettingValues.DefaultMappingXmlValue;
            }
            else
            {
                MappingHelper mh = new MappingHelper(Context);
                result[ConnectorSettings.MappingXml] = mh.GenerateXml(Context.ExtensionId);
            }
            return result;
        }

        /// <summary>
        /// INitialized listenere and returns whether listener is valid
        /// </summary>
        /// <returns>Whether the listener is valid</returns>
        protected bool initializeListener()
        {
            if (Context == null)
            {
                return false;
            }
            if (adapterSettings == null)
            {
                adapterSettings = Context.Settings;
            }
            if ((mappingHelper == null) && (Context != null))
            {
                // Perhaps use Dependency Injection for the below at some point if possible
                mappingHelper = new MappingHelper(Context);
            }
            if ((common == null) && (mappingHelper != null))
            {
                common = new CommonFunctions(mappingHelper);
            }
            if ((channelHelper == null) && (Context != null))
            {
                channelHelper = new ChannelHelper(Context);
            }
            if ((azureListenerHelper == null) && (mappingHelper != null))
            {
                azureListenerHelper = new AzureListenerHelper(mappingHelper);
            }
            if ((schemaBasedOutput == null) && (Context != null) && (mappingHelper != null))
            {
                schemaBasedOutput = new SchemaBasedOutput(Context);
                schemaBasedOutput.ExcludedFieldTypes = mappingHelper?.GetExcludedFieldTypeFromXml(adapterSettings?[ConnectorSettings.MappingXml]);
            }
            if (defaultSettings == null)
            {
                defaultSettings = getDefaultSettings();
            }
            validateListener();
            return isValid.Value;
        }

        /// <summary>
        /// Validates listener settings
        /// </summary>
        protected void validateListener()
        {
            isValid = validateSettings() && validateMappingXml();
        }

        /// <summary>
        /// Vlaidates the mapping Xml is proper
        /// </summary>
        /// <returns></returns>
        protected virtual bool validateMappingXml()
        {
            return mappingHelper.ValidateMappingXmlFromXml(adapterSettings[ConnectorSettings.MappingXml]);
        }

        /// <summary>
        /// If overriding, should call base otherwise need to check these settings as well
        /// </summary>
        /// <returns>True if all settings exist</returns>
        protected virtual bool validateSettings()
        {
            bool retVal = true;
            retVal = retVal && validateSetting(ConnectorSettings.AzureStorageConnection);
            retVal = retVal && validateSetting(ConnectorSettings.AzureContainer);
            retVal = retVal && validateSetting(ConnectorSettings.AzureEndpointUrl);
            retVal = retVal && validateSetting(ConnectorSettings.CvlFolder);
            retVal = retVal && validateSetting(ConnectorSettings.MappingXml);
            retVal = retVal && validateSetting(ConnectorSettings.MaxEntitiesInPublishedFile);
            retVal = retVal && validateSetting(ConnectorSettings.PublishAsSingleFile);
            retVal = retVal && validateSetting(ConnectorSettings.PublishFolder);
            retVal = retVal && validateSetting(ConnectorSettings.ResourceFolder);
            List<string> additionalSettings = additionalSettingsToValidate();
            if (additionalSettings != null)
            {
                foreach (string setting in additionalSettings)
                {
                    retVal =  retVal && validateSetting(setting);
                }
            }
            return retVal;
        }

        /// <summary>
        /// Validates whether a given stting has a value
        /// </summary>
        /// <returns>True if setting exists</returns>
        protected bool validateSetting(string settingName)
        {
            if (string.IsNullOrEmpty(adapterSettings?[settingName]))
            {
                Context.Log(LogLevel.Warning, $"Missing Setting {settingName}.");
                return false;
            }
            return true;
        }

    }
}
