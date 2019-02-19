using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;
using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;

namespace inRiver.SchemaBased
{
    public class SchemaBasedOutput
    {

        public SchemaBasedOutput(inRiverContext context)
        {
            this.context = context;
        }

        private readonly Dictionary<string, XmlSchema> xmlSchemas = new Dictionary<string, XmlSchema>();

        private XNamespace targetNamespace;

        private int currentChannelId;

        private Link deletedLink;

        private inRiverContext context;

        public List<string> ExcludedFieldTypes { get; set; }

        public bool IsValid(string entityTypeId, XDocument xmlDoc)
        {
            bool isValid;
            try
            {
                this.GenerateSchemaIfNotAlreadyExist(entityTypeId);
                this.ValidateXml(xmlSchemas[entityTypeId], xmlDoc, entityTypeId);
                isValid = true;
            }
            catch (Exception)
            {
                isValid = false;
            }
            return isValid;
        }

        public XDocument GenerateXml(Entity entity, SchemabasedEntityActionEnum entityAction, Dictionary<string, string> uniqueFieldTypes, int channelId = 0, Link linkToBeDeleted = null)
        {
            XDocument doc = null;
            if (entity == null)
            {
                context.Log(LogLevel.Error, "Entity argument is null in Generate Xml in SchemabasedOutput. Aborting.");
                return doc;
            }

            try
            {
                this.currentChannelId = channelId;
                this.deletedLink = linkToBeDeleted;
                ReGenerateSchema(entity.EntityType.Id, false);
                doc = this.GetDocumentBase(entity.EntityType.Id);
                XElement root = doc.Element(targetNamespace + entity.EntityType.Id + "s");
                if (root != null)
                {
                    string uniqueFieldType = string.Empty;
                    if (uniqueFieldTypes.ContainsKey(entity.EntityType.Id))
                    {
                        uniqueFieldType = uniqueFieldTypes[entity.EntityType.Id];
                    }

                    root.Add(this.CreateEntity(entity, entityAction, uniqueFieldType, uniqueFieldTypes));
                }
            }
            catch (Exception exception)
            {
                context.Log(LogLevel.Error, $"Error during generating xml for entity id {entity.Id}:", exception);
                doc = null;
            }
            return doc;
        }

        public string GenerateXml(Entity entity, SchemabasedEntityActionEnum entityAction, Dictionary<string, string> uniqueFieldTypes, bool generateNewSchema, int channelId = 0, Link linkToBeDeleted = null)
        {
            string result = string.Empty;
            if (entity == null)
            {
                context.Log(LogLevel.Error, "Entity argument is null in Generate Xml in SchemabasedOutput. Aborting.");
                return result;
            }

            try
            {
                this.currentChannelId = channelId;
                this.deletedLink = linkToBeDeleted;
                ReGenerateSchema(entity.EntityType.Id, generateNewSchema);
                XDocument doc = this.GetDocumentBase(entity.EntityType.Id);
                XElement root = doc.Element(targetNamespace + entity.EntityType.Id + "s");
                if (root != null)
                {
                    string uniqueFieldType = string.Empty;
                    if (uniqueFieldTypes.ContainsKey(entity.EntityType.Id))
                    {
                        uniqueFieldType = uniqueFieldTypes[entity.EntityType.Id];
                    }

                    XElement xElement = this.CreateEntity(entity.Id, entityAction, uniqueFieldType, uniqueFieldTypes);

                    if (xElement != null)
                    {
                        root.Add(xElement);
                    }
                }

                // Validate xml
                this.ValidateXml(xmlSchemas[entity.EntityType.Id], doc, entity.EntityType.Id);

                result = doc.ToString();
                return result;
            }
            catch (Exception exception)
            {
                context.Log(LogLevel.Error, $"Error during generating xml for entity id {entity.Id}:", exception);
                throw;
            }
        }

        public string GenerateXml(int entityId, SchemabasedEntityActionEnum entityAction, Dictionary<string, string> uniqueFieldTypes, bool generateNewSchema, int channelId = 0, Link linkToBeDeleted = null)
        {
            if (entityId == 1401 || entityId == 906)
            {
                Console.WriteLine("Test");
            }
            Entity entity = context.ExtensionManager.DataService.GetEntity(entityId, LoadLevel.Shallow);
            return GenerateXml(entity, entityAction, uniqueFieldTypes, generateNewSchema, channelId, linkToBeDeleted);
        }

        public string GenerateXml(KeyValuePair<string, List<int>> entityTypePair, SchemabasedEntityActionEnum entityAction, Dictionary<string, string> uniqueFieldTypes, bool generateNewSchema, int channelId = 0)
        {
            try
            {
                string result = string.Empty;
                this.currentChannelId = channelId;

                List<int> entityIds = entityTypePair.Value;
                if (entityIds == null || !entityIds.Any())
                {
                    context.Log(LogLevel.Error, $"Selected entities with entity type id {entityTypePair.Key} was not found when Generate Xml in SchemabasedOutput.");
                    return result;
                }

                ReGenerateSchema(entityTypePair.Key, generateNewSchema);
                var doc = this.GetDocumentBase(entityTypePair.Key);
                XElement root = doc.Element(targetNamespace + entityTypePair.Key + "s");
                if (root != null)
                {
                    foreach (int entityId in entityIds)
                    {
                        string uniqueFieldType = string.Empty;
                        if (uniqueFieldTypes.ContainsKey(entityTypePair.Key))
                        {
                            uniqueFieldType = uniqueFieldTypes[entityTypePair.Key];
                        }

                        XElement xElement = this.CreateEntity(entityId, entityAction, uniqueFieldType, uniqueFieldTypes);

                        if (xElement != null)
                        {
                            root.Add(xElement);
                        }
                    }
                }

                // Validate xml
                this.ValidateXml(xmlSchemas[entityTypePair.Key], doc, entityTypePair.Key);

                result = doc.ToString();
                return result;
            }
            catch (Exception exception)
            {
                context.Log(LogLevel.Error, $"Error during generating xml for entity type {entityTypePair.Key}:", exception);
                throw;
            }
        }

        public void ReGenerateSchema(string entityTypeId)
        {
            if (string.IsNullOrEmpty(entityTypeId))
            {
                return;
            }
            XmlSchema schema = generateSchema(entityTypeId);
            targetNamespace = XNamespace.Get(schema.TargetNamespace);
            if (xmlSchemas.ContainsKey(entityTypeId))
            {
                xmlSchemas[entityTypeId] = schema;
            }
            else
            {
                xmlSchemas.Add(entityTypeId, schema);
            }
        }

        public void ReGenerateSchema(List<string> entityTypeIds)
        {
            foreach (var entityTypeId in entityTypeIds)
            {
                ReGenerateSchema(entityTypeId);
            }
        }

        public void ReGenerateSchema(string entityTypeId, bool generateNewSchema)
        {
            if (string.IsNullOrEmpty(entityTypeId))
            {
                return;
            }
            if (xmlSchemas.ContainsKey(entityTypeId))
            {
                if (generateNewSchema)
                {
                    XmlSchema schema = generateSchema(entityTypeId);
                    targetNamespace = XNamespace.Get(schema.TargetNamespace);
                    xmlSchemas[entityTypeId] = schema;
                }
            }
            else
            {
                XmlSchema schema = generateSchema(entityTypeId);
                targetNamespace = XNamespace.Get(schema.TargetNamespace);
                xmlSchemas.Add(entityTypeId, schema);
            }
        }

        XmlSchema generateSchema(string entityTypeId)
        {
            StandardSchema generator = new StandardSchema(context.ExtensionManager);
            EntityType entityType = context.ExtensionManager.ModelService.GetEntityType(entityTypeId);
            return generator.GenerateSchemaFromEntity(entityType, LoadLevel.DataAndLinks);
        }

        private void GenerateSchemaIfNotAlreadyExist(string entityTypeId)
        {
            if (!xmlSchemas.ContainsKey(entityTypeId))
            {
                ReGenerateSchema(entityTypeId);
            }
        }

        private void ValidateXml(XmlSchema schema, XDocument doc, string entityType)
        {
            XmlSchemaSet schemas = new XmlSchemaSet();
            schemas.Add(schema);
            doc.Validate(
                schemas,
                (sender, args) =>
                {
                    context.Log(
                        LogLevel.Error,
                        $"XML Validation for entity type ({entityType}) against schema failed. {args.Message}");
                    throw args.Exception;
                });
        }

        private XElement CreateEntity(int entityId, SchemabasedEntityActionEnum entityAction, string uniqueFieldType, Dictionary<string, string> uniqueFieldTypes)
        {
            Entity entity = context.ExtensionManager.DataService.GetEntity(entityId, LoadLevel.DataAndLinks);
            if (entity == null)
            {
                return null;
            }
            return this.CreateEntity(entity, entityAction, uniqueFieldType, uniqueFieldTypes);
        }

        private XElement CreateEntity(Entity entity, SchemabasedEntityActionEnum entityAction, string uniqueFieldType, Dictionary<string, string> uniqueFieldTypes)
        {
            XElement entityElement = this.AddEntityElement(entity, entityAction, uniqueFieldType);

            // Fields
            XElement fieldsElement = new XElement(targetNamespace + $"{entity.EntityType.Id}Fields");
            entityElement.Add(fieldsElement);
            foreach (Field field in entity.Fields)
            {
                if (ExcludedFieldTypes.Contains(field.FieldType.Id))
                {
                    // Do not include this field.
                    continue;
                }

                XElement fieldElement = new XElement(targetNamespace + field.FieldType.Id);
                if (field.Data == null)
                {
                    continue;
                }

                switch (field.FieldType.DataType)
                {
                    case "LocaleString":
                        fieldElement.Add(this.AddLocaleStringFieldData(field));
                        break;
                    case "CVL":
                        if (field.FieldType.Multivalue)
                        {
                            string fieldData = field.Data.ToString();
                            var multivalues = fieldData.Split(';');

                            if (string.IsNullOrEmpty(fieldData))
                            {
                                fieldElement.Add(new XElement(this.targetNamespace + "Data", string.Empty));
                            }
                            else
                            {
                                foreach (string value in multivalues)
                                {
                                    fieldElement.Add(new XElement(this.targetNamespace + "Data", value));
                                }
                            }
                        }
                        else
                        {
                            fieldElement.Add(new XElement(targetNamespace + "Data", string.Format("{0}", field.Data)));
                        }

                        fieldElement.Add(new XAttribute("Cvl", field.FieldType.CVLId));
                        break;
                    default:
                        fieldElement.Add(new XElement(targetNamespace + "Data", string.Format("{0}", field.Data)));
                        break;
                }

                fieldsElement.Add(fieldElement);
            }

            // Links
            XElement linksElement = new XElement(targetNamespace + string.Format("{0}Links", entity.EntityType.Id));
            XElement parentLinksElement = new XElement(targetNamespace + string.Format("{0}ParentLinks", entity.EntityType.Id));
            XElement childLinksElement = new XElement(targetNamespace + string.Format("{0}ChildLinks", entity.EntityType.Id));

            foreach (Link inboundLink in context.ExtensionManager.ChannelService.GetInboundLinksForEntity(currentChannelId, entity.Id))
            {
                Entity otherEntity = context.ExtensionManager.DataService.GetEntity(inboundLink.Source.Id, LoadLevel.DataOnly);
                XElement linkElement = this.AddLinkElement(inboundLink, otherEntity, uniqueFieldTypes);
                parentLinksElement.Add(linkElement);
            }

            foreach (Link outboundLink in context.ExtensionManager.ChannelService.GetOutboundLinksForEntity(currentChannelId, entity.Id))
            {
                Entity otherEntity = context.ExtensionManager.DataService.GetEntity(outboundLink.Target.Id, LoadLevel.DataOnly);
                XElement linkElement = this.AddLinkElement(outboundLink, otherEntity, uniqueFieldTypes);
                childLinksElement.Add(linkElement);
            }

            if (deletedLink != null)
            {
                Entity otherEntity = context.ExtensionManager.DataService.GetEntity(deletedLink.Target.Id, LoadLevel.DataOnly);
                XElement linkElement = this.AddLinkElement(deletedLink, otherEntity, uniqueFieldTypes);
                linkElement.Add(new XAttribute("Action", "Deleted"));
                childLinksElement.Add(linkElement);
            }

            linksElement.Add(parentLinksElement);
            linksElement.Add(childLinksElement);
            entityElement.Add(linksElement);

            // Others
            XElement additionalsElement = new XElement(targetNamespace + string.Format("{0}Additionals", entity.EntityType.Id));
            if (entity.EntityType.Id == "Specification")
            {
                additionalsElement.Add(this.AddSpecificationTemplateElement(entity));
            }
            else if (HasSpecification(entity))
            {
                additionalsElement.Add(AddSpecificationElement(entity));
            }

            entityElement.Add(additionalsElement);
            return entityElement;
        }

        private XElement AddSpecificationTemplateElement(Entity entity)
        {
            XElement specificationTemplateElement = new XElement(this.targetNamespace + "SpecificationTemplate");
            List<Category> categories = context.ExtensionManager.DataService.GetAllSpecificationCategories();
            foreach (SpecificationFieldType specificationTemplateFieldType in context.ExtensionManager.DataService.GetSpecificationTemplateFieldTypes(entity.Id))
            {
                XElement fieldTypeElement = new XElement(this.targetNamespace + "SpecificationFieldType");

                if (specificationTemplateFieldType == null)
                {
                    continue;
                }

                if (specificationTemplateFieldType.Disabled)
                {
                    continue;
                }

                Category category = categories.FirstOrDefault(c => c.Id == specificationTemplateFieldType.CategoryId);
                if (category == null)
                {
                    continue;
                }

                XElement identityElement = new XElement(this.targetNamespace + "Id", specificationTemplateFieldType.Id);
                fieldTypeElement.Add(identityElement);

                XElement nameElement = this.AddLocaleStringToNewElement("Name", specificationTemplateFieldType.Name);
                fieldTypeElement.Add(nameElement);

                XElement dataTypeElement = new XElement(this.targetNamespace + "DataType", specificationTemplateFieldType.DataType);
                fieldTypeElement.Add(dataTypeElement);

                XElement mandatoryElement = new XElement(this.targetNamespace + "Mandatory", specificationTemplateFieldType.Mandatory);
                fieldTypeElement.Add(mandatoryElement);

                XElement indexElement = new XElement(this.targetNamespace + "Index", specificationTemplateFieldType.Index);
                fieldTypeElement.Add(indexElement);

                XElement categoryIdElement = new XElement(this.targetNamespace + "CategoryId", specificationTemplateFieldType.CategoryId);
                fieldTypeElement.Add(categoryIdElement);

                XElement categoryNameElement = this.AddLocaleStringToNewElement("CategoryName", category.Name);
                fieldTypeElement.Add(categoryNameElement);

                XElement defaultValueElement;
                if (!string.IsNullOrEmpty(specificationTemplateFieldType.DefaultValue))
                {
                    defaultValueElement = new XElement(this.targetNamespace + "DefaultValue", new XCData(specificationTemplateFieldType.DefaultValue));
                }
                else
                {
                    defaultValueElement = new XElement(this.targetNamespace + "DefaultValue");
                }

                fieldTypeElement.Add(defaultValueElement);

                XElement cvlIdElement = new XElement(this.targetNamespace + "CVLId", specificationTemplateFieldType.CVLId);
                fieldTypeElement.Add(cvlIdElement);

                XElement multivalueElement = new XElement(this.targetNamespace + "Multivalue", specificationTemplateFieldType.Multivalue);
                fieldTypeElement.Add(multivalueElement);

                XElement unitElement = new XElement(this.targetNamespace + "Unit", specificationTemplateFieldType.Unit);
                fieldTypeElement.Add(unitElement);

                XElement additionalElement = new XElement(this.targetNamespace + "Additional", specificationTemplateFieldType.AdditionalData);
                fieldTypeElement.Add(additionalElement);

                specificationTemplateElement.Add(fieldTypeElement);
            }

            return specificationTemplateElement;
        }

        private List<XElement> GetDataElements(object data, SpecificationFieldType specificationFieldType)
        {
            List<XElement> elements = new List<XElement>();

            if (data == null)
            {
                elements.Add(new XElement(this.targetNamespace + "Data"));
                return elements;
            }

            if (specificationFieldType.DataType == "LocaleString")
            {
                XElement localeElement = this.AddLocaleStringData((LocaleString)data);
                localeElement.Add(new XAttribute(targetNamespace + "type", "LocaleStringType"));
                elements.Add(localeElement);
                return elements;
            }

            if (specificationFieldType.Multivalue)
            {
                string fieldData = data.ToString();
                var multivalues = fieldData.Split(';');
                if (string.IsNullOrEmpty(fieldData))
                {
                    XElement dataSubElement = new XElement(this.targetNamespace + "Data", string.Empty);
                    elements.Add(dataSubElement);
                }
                else
                {
                    foreach (string value in multivalues)
                    {
                        if (string.IsNullOrEmpty(value))
                        {
                            continue;
                        }

                        XElement dataSubElement = new XElement(this.targetNamespace + "Data", value);
                        elements.Add(dataSubElement);
                    }
                }
            }
            else
            {
                XElement dataElement = new XElement(this.targetNamespace + "Data", new XCData(data.ToString()));
                elements.Add(dataElement);
            }

            if (specificationFieldType.DataType == DataType.CVL)
            {
                foreach (XElement element in elements)
                {
                    if (element.Name.LocalName == "Data")
                    {
                        element.Add(new XAttribute(targetNamespace + "type", "StringType"));
                        element.Add(new XAttribute("cvl", specificationFieldType.CVLId));
                    }
                }
            }

            return elements;
        }

        private XElement AddSpecificationElement(Entity entity)
        {
            XElement specificationElement = new XElement(this.targetNamespace + "SpecificationData");
            specificationElement.Add(new XAttribute(XNamespace.Xmlns + "tns", targetNamespace));

            foreach (SpecificationField field in context.ExtensionManager.DataService.GetSpecificationFieldsForEntity(entity.Id))
            {
                if (field == null)
                {
                    continue;
                }

                if (field.SpecificationFieldType == null)
                {
                    continue;
                }

                XElement row = new XElement(this.targetNamespace + "Row");
                XElement specificationFieldTypeIdElement = new XElement(this.targetNamespace + "SpecificationFieldTypeId", field.SpecificationFieldType.Id);
                row.Add(specificationFieldTypeIdElement);

                if (field.Formatted)
                {
                    string value = context.ExtensionManager.DataService.GetFormattedValue(field.SpecificationFieldType, entity.Id);

                    if (string.IsNullOrEmpty(value))
                    {
                        XElement valueElement = new XElement(this.targetNamespace + "Data");
                        row.Add(valueElement);
                    }
                    else
                    {
                        XElement valueElement = new XElement(this.targetNamespace + "Data", new XCData(value));
                        row.Add(valueElement);
                    }

                    specificationElement.Add(row);
                    continue;
                }

                foreach (XElement dataElement in GetDataElements(field.Data, field.SpecificationFieldType))
                {
                    row.Add(dataElement);
                }

                specificationElement.Add(row);
            }

            return specificationElement;
        }

        private bool HasSpecification(Entity entity)
        {
            if (entity.LoadLevel != LoadLevel.DataAndLinks)
            {
                entity.Links = context.ExtensionManager.DataService.GetOutboundLinksForEntity(entity.Id);
            }

            if (entity.OutboundLinks.FirstOrDefault(l => l.Target.EntityType.Id == "Specification") != null)
            {
                return true;
            }

            return false;
        }

        private XElement AddLinkElement(Link link, Entity otherEntity, Dictionary<string, string> uniqueFieldTypes)
        {
            XElement linkElement = new XElement(
                this.targetNamespace + link.LinkType.Id,
                new XAttribute("SourceEntityTypeId", link.Source.EntityType.Id),
                new XAttribute("TargetEntityTypeId", link.Target.EntityType.Id));
            KeyValuePair<string, object> uniqueFieldPair = this.GetUniqueFieldTypeId(otherEntity, uniqueFieldTypes);
            linkElement.Add(new XAttribute("UniqueFieldName", uniqueFieldPair.Key));
            linkElement.Add(new XAttribute("UniqueValue", uniqueFieldPair.Value));
            linkElement.Add(otherEntity.Id == link.Source.Id ? new XAttribute("SourceEntityId", otherEntity.Id) : new XAttribute("TargetEntityId", otherEntity.Id));

            if (link.LinkEntity != null)
            {
                linkElement.Add(new XAttribute("LinkEntityId", link.LinkEntity.Id));
                linkElement.Add(new XAttribute("LinkEntityTypeId", link.LinkEntity.EntityType.Id));
            }

            linkElement.Add(new XAttribute("SortOrder", link.Index));

            return linkElement;
        }

        private KeyValuePair<string, object> GetUniqueFieldTypeId(Entity entity, Dictionary<string, string> uniqueFieldTypes)
        {
            string entityTypeId = entity.EntityType.Id;
            if (!uniqueFieldTypes.ContainsKey(entityTypeId))
            {
                // No unique field exist. Use EntityId instead.
                return new KeyValuePair<string, object>("EntityId", entity.Id);
            }

            KeyValuePair<string, object> result = new KeyValuePair<string, object>(string.Empty, null);
            foreach (Field field in entity.Fields)
            {
                if (field.FieldType.Id != uniqueFieldTypes[entityTypeId])
                {
                    continue;
                }

                if (field.Data != null)
                {
                    result = new KeyValuePair<string, object>(field.FieldType.Id, field.Data);
                    break;
                }
            }

            if (string.IsNullOrEmpty(result.Key))
            {
                // No unique field exist. Use EntityId instead.
                result = new KeyValuePair<string, object>("EntityId", entity.Id);
            }

            return result;
        }

        private XElement AddLocaleStringFieldData(Field field)
        {
            XElement dataElement = new XElement(targetNamespace + "Data");
            LocaleString ls = field.Data as LocaleString;
            IList<CultureInfo> languages = context.ExtensionManager.UtilityService.GetAllLanguages();

            if (ls != null)
            {
                foreach (CultureInfo ci in ls.Languages)
                {
                    foreach (CultureInfo ciLanguage in languages)
                    {
                        if (ciLanguage.Name == ci.Name)
                        {
                            XElement localeStringElement = new XElement(targetNamespace + "LocaleString");
                            localeStringElement.Add(new XElement(targetNamespace + "Language", ci.Name));
                            localeStringElement.Add(new XElement(targetNamespace + "Value", ls[ci]));
                            dataElement.Add(localeStringElement);
                            break;
                        }
                    }
                }
            }

            return dataElement;
        }

        private XElement AddLocaleStringToNewElement(string elementName, LocaleString ls)
        {
            XElement dataElement = new XElement(this.targetNamespace + elementName);
            if (ls != null)
            {
                foreach (CultureInfo ci in ls.Languages)
                {
                    XElement localeStringElement = new XElement(targetNamespace + "LocaleString");
                    localeStringElement.Add(new XElement(targetNamespace + "Language", ci.Name));
                    localeStringElement.Add(new XElement(targetNamespace + "Value", new XCData(ls[ci])));
                    dataElement.Add(localeStringElement);
                }
            }

            return dataElement;
        }

        private XElement AddLocaleStringData(LocaleString ls)
        {
            XElement dataElement = new XElement(this.targetNamespace + "Data");
            if (ls != null)
            {
                foreach (CultureInfo ci in ls.Languages)
                {
                    XElement localeStringElement = new XElement(targetNamespace + "LocaleString");
                    localeStringElement.Add(new XElement(targetNamespace + "Language", ci.Name));
                    localeStringElement.Add(new XElement(targetNamespace + "Value", new XCData(ls[ci])));
                    dataElement.Add(localeStringElement);
                }
            }

            return dataElement;
        }

        private XElement AddEntityElement(Entity entity, SchemabasedEntityActionEnum entityAction, string uniqueFieldType)
        {
            XElement element = new XElement(targetNamespace + entity.EntityType.Id);
            element.Add(new XAttribute("EntityId", entity.Id));

            if (!string.IsNullOrEmpty(entity.FieldSetId) && entity.EntityType.FieldSets.Any(et => et.Id == entity.FieldSetId))
            {
                element.Add(new XAttribute("FieldSet", entity.FieldSetId));
            }

            switch (entityAction)
            {
                case SchemabasedEntityActionEnum.New:
                    element.Add(new XAttribute("Action", "New"));
                    break;
                case SchemabasedEntityActionEnum.Updated:
                    element.Add(new XAttribute("Action", "Updated"));
                    break;
                case SchemabasedEntityActionEnum.Deleted:
                    element.Add(new XAttribute("Action", "Deleted"));
                    break;
            }

            if (!string.IsNullOrEmpty(uniqueFieldType))
            {
                element.Add(new XAttribute("ExternalUniqueIdField", uniqueFieldType));
            }

            return element;
        }

        private XDocument GetDocumentBase(string entityTypeId)
        {
            var doc = new XDocument(new XDeclaration("1.0", "utf-8", null));

            if (targetNamespace == null)
            {
                context.Log(LogLevel.Debug, $"Error: Schema not generated for type {entityTypeId}");
                ReGenerateSchema(entityTypeId);
            }

            var root = new XElement(targetNamespace + entityTypeId + "s");
            doc.Add(root);
            return doc;
        }
    }
}