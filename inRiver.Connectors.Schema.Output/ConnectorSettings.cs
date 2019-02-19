namespace inRiver.Connectors.Schema.Output
{
    internal struct ConnectorSettings
    {
        internal const string AzureStorageConnection = "AZURE_STORAGE_CONNECTION";
        internal const string AzureEndpointUrl = "AZURE_ENDPOINT_URL";
        internal const string AzureContainer = "AZURE_CONTAINER";
        internal const string AzureStoreageMedium = "AZURE_STORAGE_MEDIUM";         // not exposed but will toggle between Blob and file storage, defaults to file
        internal const string ChannelId = "CHANNEL_ID";            
        internal const string PublishFolder = "PUBLISH_FOLDER";
        internal const string ResourceFolder = "RESOURCE_FOLDER";
        internal const string MappingXml = "MAPPING_XML";
        internal const string PublishAsSingleFile = "PUBLISH_AS_SINGLE_FILE";
        internal const string MaxEntitiesInPublishedFile = "MAX_ENTITIES_IN_PUBLISHED_FILE";
        internal const string CvlFolder = "CVL_FOLDER";
        //internal const string FtpFallbackFolder = "FTP_FALLBACK_FOLDER";            
        //internal const string FtpServerUri = "FTP_SERVER_URI";            
        //internal const string FtpServerFolder = "FTP_SERVER_FOLDER";
        //internal const string FtpUserName = "FTP_USERNAME";            
        //internal const string FtpPassword = "FTP_PASSWORD";
        internal const string UpdateParentWhenAddEntity= "UPDATE_PARENT_WHEN_ADD_ENTITY";

    }

    internal struct ConnectorSettingValues
    {
        internal const string DefaultMappingXmlValue =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<mapping>  
    <externalUniqueFieldTypes>
        <externalUniqueFieldType entityTypeId=""Channel"" fieldTypeId=""ChannelName""/>     
        <externalUniqueFieldType entityTypeId=""ChannelNode"" fieldTypeId=""ChannelNodeId""/>        
        <externalUniqueFieldType entityTypeId=""Item"" fieldTypeId=""StyleNumber""/>           
        <externalUniqueFieldType entityTypeId=""Product"" fieldTypeId=""ArtNumber""/>              
    </externalUniqueFieldTypes>
    <imageConfigurations>
        <imageConfiguration name=""Thumbnail""/>
        <imageConfiguration name=""Preview""/>                   
        <imageConfiguration name=""SmallThumbnail""/>
        <imageConfiguration name=""Original""/>
    </imageConfigurations>
    <imageExtensions>
        <imageExtension name=""jpg""/>
        <imageExtension name=""jpeg""/>
        <imageExtension name=""gif""/>
        <imageExtension name=""png""/>
        <imageExtension name=""tif""/>
        <imageExtension name=""tiff""/>
    </imageExtensions>
    <entityTypesToExport>
        <entityToExport id=""Channel""/>
        <entityToExport id=""ChannelNode""/>
        <entityToExport id=""Item""/>
        <entityToExport id=""Product""/>
        <entityToExport id=""Resource""/>
    </entityTypesToExport>
    <excludedFieldTypes>
        <!-- < excludedFieldType id = ""ResourceFileName"" /> -->

    </excludedFieldTypes>
</mapping>";


    }
}