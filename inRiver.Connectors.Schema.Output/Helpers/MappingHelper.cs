using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;

namespace inRiver.Connectors.Schema.Output.Helpers
{


    internal class MappingHelper : ExtensionHelper, IExtensionHelper
    {


        private const int NumberOfGroupElements = 2;

        private static readonly List<string> EntityTypesToExclude = new List<string> { "Section", "Publication", "Task", "Assortment" };

        internal MappingHelper(inRiverContext context)
            : base(context)
        {
            this.Context = context;
        }

        internal string GenerateXmlFile(string connectorId)
        {
            XDocument doc =
                XDocument.Parse(@"<mapping>
                                    <externalUniqueFieldTypes />
                                    <entityTypesToExport />
                                    <imageConfigurations />
                                    <excludedFieldTypes>
                                      <!-- <excludedFieldType id=""ResourceFileName"" /> -->
                                    </excludedFieldTypes>
                                  </mapping>");
            if (doc.Root != null)
            {
                SetDefaultExternalUniqueFieldTypeMapping(doc.Root);
                SetDefaultImageConfigurations(doc.Root);
                SetDefaultEntityTypeToExport(doc.Root);
            }
            // TODO - Replace with memory or cloud storage
            string executionFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(executionFolder))
            {
                executionFolder = @"C:\Temp";
            }

            string path = Path.Combine(
                executionFolder,
                string.Format("inRiver.Connectors.Schema.Output.Mapping.{0}.xml", connectorId));

            doc.Save(path);
            return path;
        }

        internal string GenerateXml(string connectorId)
        {
            XDocument doc =
                XDocument.Parse(@"<mapping>
                                    <externalUniqueFieldTypes />
                                    <entityTypesToExport />
                                    <imageConfigurations />
                                    <excludedFieldTypes>
                                      <!-- <excludedFieldType id=""ResourceFileName"" /> -->
                                    </excludedFieldTypes>
                                  </mapping>");
            if (doc.Root != null)
            {
                SetDefaultExternalUniqueFieldTypeMapping(doc.Root);
                SetDefaultImageConfigurations(doc.Root);
                SetDefaultEntityTypeToExport(doc.Root);
            }
            return doc.ToString();
        }
        
        /// <summary>
        /// Will eventually be obsolete for cloud implementation
        /// </summary>
        /// <param name="path">Path to mapping file</param>
        /// <param name="connectorId">Connector Id</param>
        /// <param name="sessionGuid">Session Id</param>
        /// <returns></returns>
        internal bool ValidateMappingXmlFromFile(string path)
        {
            try
            {
                XDocument doc = XDocument.Load(path);
                return ValidateMappingXml(doc);
            }
            catch (Exception exception)
            {

                Context.Log(LogLevel.Error, "Error in ValidateMappingXmlFromFile", exception);
                //EventHelper.FireEvent(null, connectorId, ConnectorEventType.Start, 10, "Error while validating MappingXml: " + exception.Message, sessionGuid, true);
                return false;
            }
        }

        internal bool ValidateMappingXmlFromXml(string xml)
        {
            try
            {
                XDocument doc = XDocument.Parse(xml);
                return ValidateMappingXml(doc);
            }
            catch (Exception exception)
            {

                Context.Log(LogLevel.Error, "Error in ValidateMappingXmlFromFile", exception);
                //EventHelper.FireEvent(null, connectorId, ConnectorEventType.Start, 10, "Error while validating MappingXml: " + exception.Message, sessionGuid, true);
                return false;
            }
        }

        bool ValidateMappingXml(XDocument doc)
        {
            try
            {
                if (doc.Root == null)
                {
                    Context.Log(LogLevel.Error, "MappingXml root element does not exist.");
                    return false;
                }

                int count = NumberOfGroupElements;
                foreach (XElement element in doc.Root.Elements())
                {
                    switch (element.Name.LocalName)
                    {
                        case "externalUniqueFieldTypes":
                        case "entityTypesToExport":
                            --count;
                            break;
                    }
                }

                if (0 == count)
                {
                    return true;
                }
                // ReSharper disable once RedundantIfElseBlock
                else
                {
                    Context.Log(LogLevel.Error, "Error while validating MappingXml critical element missing (entitytTypeToExports or externalUniueFieldTypes)");
                    //EventHelper.FireEvent(null, connectorId, ConnectorEventType.Start, 10, "Error while validating MappingXml", sessionGuid, true);
                    return false;
                }
            }
            catch (Exception exception)
            {

                Context.Log(LogLevel.Error, "Error in ValidateMappingXml", exception);
                //EventHelper.FireEvent(null, connectorId, ConnectorEventType.Start, 10, "Error while validating MappingXml: " + exception.Message, sessionGuid, true);
                return false;
            }
        }

        internal List<string> GetExcludedFieldTypeFromFile(string path)
        {
            XDocument doc = XDocument.Load(path);
            if (doc.Root == null)
            {
                Context.Log(LogLevel.Warning,
                    string.Format("Can't find the connector mapping file, the path is {0}", path));
                return new List<string>();
            }
            return GetExcludedFieldType(doc);
        }

        internal List<string> GetExcludedFieldTypeFromXml(string xml)
        {
            if (string.IsNullOrEmpty(xml))
            {
                return new List<string>();
            }
            XDocument doc = XDocument.Parse(xml);
            return GetExcludedFieldType(doc);
        }

        internal List<string> GetExcludedFieldType(XDocument doc)
        {
            List<string> result = new List<string>();


            XElement groupElement = doc.Root.Element("excludedFieldTypes");
            if (groupElement == null)
            {
                Context.Log(LogLevel.Warning, "Can't find excluded field types. The connector mapping file does not contain element with the name 'excludedFieldTypes'.");
                return result;
            }

            result.AddRange(
                from element in groupElement.Elements()
                where element.HasAttributes && element.Attribute("id") != null
                select element.Attribute("id").Value);

            return result;
        }

        internal List<string> GetEntityTypeIdToExportFromFile(string path)
        {
            XDocument doc = XDocument.Load(path);
            if (doc.Root == null)
            {
                Context.Log(
                    LogLevel.Warning,
                    string.Format("Can't find the connector mapping file, the path is {0}", path));
                return new List<string>();
            }
            return GetEntityTypeIdToExport(doc);
        }

        internal List<string> GetEntityTypeIdToExportFromXml(string xml)
        {
            XDocument doc = XDocument.Parse(xml);
            return GetEntityTypeIdToExport(doc);
        }

        internal List<string> GetEntityTypeIdToExport(XDocument doc)
        {
            List<string> result = new List<string>();


            XElement groupElement = doc?.Root?.Element("entityTypesToExport");
            if (groupElement == null)
            {
                Context.Log(
                    LogLevel.Warning,
                    "Can't find entity types to export. The connector mapping does not contain element with the name 'entityTypesToExport'.");
                return result;
            }

            result.AddRange(
                from element in groupElement.Elements()
                where element.HasAttributes && element.Attribute("id") != null
                select element.Attribute("id").Value);

            return result;
        }


        internal List<string> GetImageConfigurationsToExportFromFile(string path)
        {
            XDocument doc = XDocument.Load(path);
            if (doc.Root == null)
            {
                Context.Log(LogLevel.Warning, string.Format("Can't find the connector mapping file, the path is {0}", path));
                return new List<string>();
            }
            return GetImageConfigurationsToExport(doc);
        }

        internal List<string> GetImageConfigurationsToExportFromXml(string xml)
        {
            XDocument doc = XDocument.Parse(xml);
            return GetImageConfigurationsToExport(doc);
        }

        internal List<string> GetImageConfigurationsToExport(XDocument doc)
        {
            List<string> result = new List<string>();
            XElement groupElement = doc?.Root?.Element("imageConfigurations");
            if (groupElement == null)
            {
                Context.Log(LogLevel.Warning, "Can't find image Configurations to export. The connector mapping does not contain element with the name 'imageConfigurations'.");
                return result;
            }

            result.AddRange(
                from element in groupElement.Elements()
                where element.HasAttributes && element.Attribute("name") != null
                select element.Attribute("name").Value);

            return result;
        }


        internal Dictionary<string, string> GetExternalUniqueFieldTypesFromFile(string path)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            XDocument doc = XDocument.Load(path);
            if (doc.Root == null)
            {
                Context.Log(
                    LogLevel.Warning,
                    string.Format("Can't find the connector mapping file, the path is {0}", path));
                return new Dictionary<string, string>();
            }
            return GetExternalUniqueFieldTypes(doc);
        }


        internal Dictionary<string, string> GetExternalUniqueFieldTypesFromXml(string xml)
        {
            XDocument doc = XDocument.Parse(xml);
            return GetExternalUniqueFieldTypes(doc);
        }

        internal Dictionary<string, string> GetExternalUniqueFieldTypes(XDocument doc)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            XElement typesElement = doc.Root.Element("externalUniqueFieldTypes");
            if (typesElement == null)
            {
                Context.Log(
                    LogLevel.Warning,
                    "Can't find external unique field types. The connector mapping file does not contain element with the name 'externalUniqueFieldTypes'.");
                return result;
            }

            foreach (XElement element in typesElement.Elements())
            {
                if (!element.HasAttributes || element.Attribute("entityTypeId") == null
                    || element.Attribute("fieldTypeId") == null)
                {
                    // Bad content
                    continue;
                }

                string entityTypeId = element.Attribute("entityTypeId").Value;
                string fieldTypeId = element.Attribute("fieldTypeId").Value;

                if (result.ContainsKey(entityTypeId))
                {
                    Context.Log(
                        LogLevel.Warning,
                        string.Format("Entity Type {0} have multiple unique field types. Ignore all except the first.", entityTypeId));
                    continue;
                }

                result.Add(entityTypeId, fieldTypeId);
            }

            return result;
        }



        private void SetDefaultExternalUniqueFieldTypeMapping(XElement root)
        {
            XElement element = root.Element("externalUniqueFieldTypes");
            if (element == null)
            {
                Context.Log(
                    LogLevel.Warning,
                    "Can't set default external unique field types. The connector mapping file does not contain element with the name 'externalUniqueFieldTypes'.");
                return;
            }

            List<EntityType> entityTypes = Context.ExtensionManager.ModelService.GetAllEntityTypes();
            foreach (EntityType entityType in entityTypes)
            {
                foreach (FieldType fieldType in entityType.FieldTypes)
                {
                    if (!fieldType.Unique)
                    {
                        continue;
                    }

                    XElement content = new XElement(
                        "externalUniqueFieldType",
                        new XAttribute("entityTypeId", entityType.Id),
                        new XAttribute("fieldTypeId", fieldType.Id));
                    element.Add(content);
                }
            }
        }

        private void SetDefaultImageConfigurations(XElement root)
        {
            XElement element = root.Element("imageConfigurations");
            if (element == null)
            {
                Context.Log(
                    LogLevel.Error,
                    "Can't set default image configurations. The connector mapping file does not contain element with the name 'imageConfigurations'.");
                return;
            }

            List<string> names = Context.ExtensionManager.UtilityService.GetAllImageConfigurations();
            foreach (string name in names)
            {
                XElement imageConfigurationElement = new XElement("imageConfiguration", new XAttribute("name", name));
                element.Add(imageConfigurationElement);
            }
        }

        private void SetDefaultEntityTypeToExport(XElement root)
        {
            XElement element = root.Element("entityTypesToExport");
            if (element == null)
            {
                Context.Log(
                    LogLevel.Error,
                    "Can't set default entity type to export. The connector mapping file does not contain element with the name 'entityTypesToExport'.");
                return;
            }

            List<EntityType> entityTypes = Context.ExtensionManager.ModelService.GetAllEntityTypes();
            foreach (EntityType entityType in entityTypes)
            {
                if (!EntityTypesToExclude.Contains(entityType.Id))
                {
                    element.Add(new XElement("entityToExport", new XAttribute("id", entityType.Id)));
                }
            }
        }
    }
}