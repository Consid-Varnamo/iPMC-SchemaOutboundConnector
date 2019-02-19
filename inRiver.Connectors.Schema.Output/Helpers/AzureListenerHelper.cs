using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;

using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Blob;

using LogLevel = inRiver.Remoting.Log.LogLevel;

namespace inRiver.Connectors.Schema.Output.Helpers
{
    /// <summary>
    /// Helper class for Azure Listeners
    /// </summary>
    internal class AzureListenerHelper : ExtensionHelper, IExtensionHelper
    {

        private readonly MappingHelper mappingFileHelper;

        private AzureStorageMedium storageMedium;

        internal AzureListenerHelper(MappingHelper mappingHelper)
            : base(mappingHelper)
        {
            this.mappingFileHelper = mappingHelper;
            this.Context = mappingHelper.Context;
            setStorageMedium();
        }

        internal void StoreResourceFile(int fileId, string fileName, string displayConfiguration, string resourceFolderPath)
        {
            AzureEnvironmentInfo abei = new AzureEnvironmentInfo(Context);
            byte[] file;
            try
            {
                file = Context.ExtensionManager.UtilityService.GetFile(fileId, displayConfiguration);
                if (file == null)
                {
                    Context.Log(LogLevel.Error, $"Error in StoreResourceFile, File with Id {fileId} and displayConfiguration:{displayConfiguration} could not be retrieved!");
                    return;
                }
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, $"Error in StoreResourceFile, File with Id {fileId} and displayConfiguration:{displayConfiguration} could not be retrieved!", ex);
                return;
            }

            var fileNamePath = $"{fileId}_{fileName}";
            var configPath = $@"{resourceFolderPath}/{displayConfiguration}";

            var filePath = $@"{configPath}/{fileNamePath}";
            uploadFileToAzure(file, filePath);
        }

        internal void PublishEntityTypeXml(string entityTypeId, XDocument document, string mainFolder, int countNumber = 0)
        {
            try
            {
                DateTime now = DateTime.Now;

                string numberString = string.Empty;
                if (countNumber > 0)
                {
                    numberString = $"-{countNumber.ToString(CultureInfo.InvariantCulture)}";
                }

                string fileName = $@"{now.ToString("O")}_{entityTypeId}_Published{numberString}.xml";
                fileName = fileName.Replace(":", ".");
                string filePath = $@"{mainFolder}/{fileName}";
                uploadFileToAzure(document, filePath);
            }
            catch (Exception exception)
            {
                Context.Log(LogLevel.Error, $"Error in PublishEntityTypeXml: \n{exception}");
            }
        }

        internal bool WriteEntityXmlToBlobFile(string entityTypeId, string uniqueId, XDocument document, string folder)
        {
            try
            {
                DateTime now = DateTime.Now;

                string fileName = $@"{now.ToString("O")}_{entityTypeId}_{uniqueId}.xml";
                fileName = fileName.Replace(":", ".").Replace("\\", "_");
                string filePath = $@"{folder}/{fileName}";
                uploadFileToAzure(document, filePath);
                return true;
            }
            catch (Exception exception)
            {
                Context.Log(LogLevel.Error, $"Error in WriteEntityXmlToBlobFile ", exception);
                return false;
            }
        }


        void setStorageMedium()
        {
            if (Context.Settings.ContainsKey(ConnectorSettings.AzureStoreageMedium))
            {
                if (!Enum.TryParse<AzureStorageMedium>(Context.Settings[ConnectorSettings.AzureStoreageMedium], out storageMedium))
                {
                    storageMedium = AzureStorageMedium.File;
                }
            }
            else
            {
                storageMedium = AzureStorageMedium.File;
            }
        }

        internal int StoreResourceFiles(List<int> entities, Dictionary<string, string> adapterSettings)
        {
            var amountStored = 0;
            const string OriginalDisplayConfigurationName = "Original";
            try
            {
                string path = adapterSettings[ConnectorSettings.ResourceFolder];
                var configurations = this.mappingFileHelper.GetImageConfigurationsToExportFromXml(adapterSettings[ConnectorSettings.MappingXml]);
                var imageServiceConfigurations = Context.ExtensionManager.UtilityService.GetAllImageServiceConfigurations();

                foreach (int entityId in entities)
                {
                    var resource = Context.ExtensionManager.DataService.GetEntity(entityId, LoadLevel.DataOnly);
                    var identityField = resource.GetField("ResourceFileId");
                    var nameField = resource.GetField("ResourceFilename");

                    if (identityField == null || identityField.Data == null || nameField == null || nameField.Data == null)
                    {
                        // No file to store for this resource.
                        Context.Log(LogLevel.Information,  $"No file stored for resource with id {entityId}, as it did not have valid data in one or both of the fields ResourceFileId/ResourceFilename");
                        continue;
                    }

                    var id = int.Parse(identityField.Data.ToString());
                    var fileName = nameField.Data.ToString();
                    var fileExtension = Path.GetExtension(fileName).Replace(".", string.Empty).ToLower(CultureInfo.InvariantCulture);

                    if (imageServiceConfigurations.All(config => config.Extension != fileExtension))
                    {
                        StoreResourceFile(id, fileName, OriginalDisplayConfigurationName, path);
                        amountStored++;
                    }
                    else
                    {
                        foreach (var configuration in configurations)
                        {
                            var outputExtension = string.Empty;
                            var imageConfigExist = false;

                            if (configuration != OriginalDisplayConfigurationName)
                            {
                                var imageConfig = imageServiceConfigurations.FirstOrDefault(c => c.Extension == fileExtension && c.Name == configuration);

                                if (imageConfig == null)
                                {
                                    Context.Log(LogLevel.Information, $"The configuration {configuration} with the extension {fileExtension} doesn't exist in the system. So the resource file {fileName} can't be saved in resources!");
                                    continue;
                                }
                                else
                                {
                                    imageConfigExist = true;
                                    outputExtension = imageConfig.OutputExtension;
                                }
                            }

                            if (imageConfigExist)
                            {
                                var filenameNameWithConfigurationExtension = GetFilenameWithExtension(fileName, outputExtension);
                                StoreResourceFile(id, filenameNameWithConfigurationExtension, configuration, path);
                            }
                        }

                        amountStored++;
                    }
                }
            }
            catch (Exception exception)
            {
                Context.Log(LogLevel.Error, $"Error in StoreResourceFiles ", exception);
            }
            return amountStored;
        }

        internal void WriteCvlForEntityType(EntityType entityType, string cvlFolder)
        {
            try
            {
                List<CVL> cvls = (from fieldType in entityType.FieldTypes
                                  where !string.IsNullOrEmpty(fieldType.CVLId)
                                  select Context.ExtensionManager.ModelService.GetCVL(fieldType.CVLId)).ToList();

                if (entityType.Id == "Specification")
                {
                    List<SpecificationFieldType> specificationFieldTypes = Context.ExtensionManager.DataService.GetAllSpecificationFieldTypes();
                    foreach (SpecificationFieldType fieldType in specificationFieldTypes)
                    {
                        if (string.IsNullOrEmpty(fieldType.CVLId))
                        {
                            continue;
                        }

                        if (cvls.Any(p => p.Id == fieldType.CVLId))
                        {
                            // Already included and will be exported.
                            continue;
                        }

                        var cvl = Context.ExtensionManager.ModelService.GetCVL(fieldType.CVLId);
                        if (cvl != null)
                        {
                            cvls.Add(cvl);
                        }
                        else
                        {
                            Context.Log(LogLevel.Error, $"Error when trying to add CVL with Id: {fieldType.CVLId}. The CVL couldn't be found in your model. \n");
                        }
                    }
                }

                foreach (CVL cvl in cvls)
                {
                    XDocument doc = GenerateXmlForCvl(cvl);
                    string fileName = $@"{cvlFolder}/{cvl.Id}.xml";
                    uploadFileToAzure(doc, fileName);
                }
            }
            catch (Exception exception)
            {
                Context.Log(LogLevel.Error, $"Error in WriteCvlForEntityType for entity type {entityType.Id} \n{exception}");
            }
        }

        internal void WriteCvlById(string cvlId, string action, string cvlFolder)
        {
            try
            {
                CVL cvl = Context.ExtensionManager.ModelService.GetCVL(cvlId);
                XDocument doc = GenerateXmlForCvl(cvl, action);
                string fileName = $@"{cvlFolder}/{cvl.Id}.xml";
                uploadFileToAzure(doc, fileName);
            }
            catch (Exception exception)
            {
                Context.Log(LogLevel.Error, $"Error in WriteCvlById for cvl id {cvlId} \n{exception}");
                throw;
            }
        }

        internal XDocument GenerateXmlForCvl(CVL cvl, string action = "")
        {
            XDocument doc = new XDocument(new XDeclaration("1.0", "utf-8", null));
            var root = new XElement(
                "CVL",
                new XAttribute("CustomValueList", cvl.CustomValueList),
                new XAttribute("DataType", cvl.DataType),
                new XAttribute("Id", cvl.Id));
            if (!string.IsNullOrEmpty(action))
            {
                root.Add(new XAttribute("Action", action));
            }

            if (!string.IsNullOrEmpty(cvl.ParentId))
            {
                root.Add(new XAttribute("ParentId", cvl.ParentId));
            }

            List<CVLValue> cvlValues = Context.ExtensionManager.ModelService.GetCVLValuesForCVL(cvl.Id);
            foreach (CVLValue value in cvlValues)
            {
                root.Add(XElement.Parse(value.ToXml()));
            }

            doc.Add(root);
            return doc;
        }

        internal string GetFilenameWithExtension(string filename, string extension)
        {
            if (string.IsNullOrEmpty(extension))
            {
                return filename;
            }

            string filenameWithoutExtension = Path.GetFileNameWithoutExtension(filename);

            return $"{filenameWithoutExtension}.{extension}";
        }

        /// <summary>
        /// Saves a blob, will overwrite if it exists
        /// </summary>
        /// <param name="blob">Byte array of blob to save</param>
        /// <param name="fileName">Filename / path inside container to save</param>
        /// <returns>URL of saved blob</returns>
        string uploadFileToAzure(byte[] file, string fileName)
        {
            AzureEnvironmentInfo aei = new AzureEnvironmentInfo(Context);
            AzureStorageHelper storage = new AzureStorageHelper(storageMedium, aei);
            return storage.uploadFile(file, fileName);
        }

        /// <summary>
        /// Saves a blob, will overwrite if it exists
        /// </summary>
        /// <param name="doc">XDocument to save</param>
        /// <param name="fileName">Filename / path inside container to save</param>
        /// <returns>URL of saved blob</returns>
        string uploadFileToAzure(XDocument doc, string fileName)
        {
            MemoryStream ms = new MemoryStream();
            doc.Save(ms);
            return uploadFileToAzure(ms.ToArray(), fileName);
        }

    }
}