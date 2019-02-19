using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using inRiver.Remoting;
using inRiver.Remoting.Objects;

namespace inRiver.SchemaBased
{

    public class StandardSchema
    {
        private const string InriverNamespace = "http://www.inriver.com/pim/6/std/3.0";
        private const string W3Namespace = "http://www.w3.org/2001/XMLSchema";

        private readonly IinRiverManager manager;

        public StandardSchema(IinRiverManager manager)
        {
            this.manager = manager;
        }

        public XmlSchema GenerateSchemaFromEntity(EntityType entityType, LoadLevel loadLevel)
        {
            XmlSchema schema = new XmlSchema
            {
                TargetNamespace = InriverNamespace,
                ElementFormDefault = XmlSchemaForm.Qualified
            };

            if (entityType == null)
            {
                return schema;
            }

            schema.Items.Add(
                new XmlSchemaElement
                {
                    Name = string.Format("{0}s", entityType.Id),
                    SchemaTypeName =
                        new XmlQualifiedName(
                        string.Format("{0}s", entityType.Id),
                        InriverNamespace)
                });

            // The different own-defined types.
            XmlSchemaComplexType entitiesComplexType = this.GetEntitiesComplexType(entityType);
            schema.Items.Add(entitiesComplexType);

            XmlSchemaComplexType entityComplexType = this.GetEntityComplexType(entityType);
            schema.Items.Add(entityComplexType);

            XmlSchemaComplexType fieldListComplexType = this.GetFieldsComplexType(entityType);
            schema.Items.Add(fieldListComplexType);

            XmlSchemaComplexType linkListComplexType = this.GetLinksComplexType(entityType.Id);
            schema.Items.Add(linkListComplexType);

            XmlSchemaComplexType subLinksComplexType = this.GetSubLinksComplexType(entityType, true);
            schema.Items.Add(subLinksComplexType);

            subLinksComplexType = this.GetSubLinksComplexType(entityType, false);
            schema.Items.Add(subLinksComplexType);

            XmlSchemaComplexType additionaslComplexType = this.GetAdditionalComplexType(entityType);
            schema.Items.Add(additionaslComplexType);

            schema.Items.Add(this.GetSpecificationDataComplexType());
            schema.Items.Add(this.GetSpecificationDataRowComplexType());
            schema.Items.Add(this.GetSpecificationDataRowTypes());
            schema.Items.Add(this.GetSpecificationTemplateComplexType());
            schema.Items.Add(this.GetSpecificationFieldTypeCompleteType());

            XmlSchemaComplexType localStringComplexType = this.GetLocalStringComplexType();
            schema.Items.Add(localStringComplexType);

            XmlSchemaSimpleType fieldSetSimpleType = this.GetFieldSetSimpleType(entityType);
            schema.Items.Add(fieldSetSimpleType);

            XmlSchemaSimpleType actionSimpleType = this.GetEntityActionSimpleType();
            schema.Items.Add(actionSimpleType);

            XmlSchemaSimpleType linkActionSimpleType = this.GetLinkActionSimpleType();
            schema.Items.Add(linkActionSimpleType);

            XmlSchemaSimpleType languageSimpleType = this.GetLanguageSimpleType();
            schema.Items.Add(languageSimpleType);

            XmlSchemaSimpleType externalUniqueIdSimpleType = this.GetExternalUniqueIdFieldSimpleType(entityType);
            schema.Items.Add(externalUniqueIdSimpleType);

            XmlSchemaSimpleType cvlIdSimpletype = this.GetCvlIdSimpleType(entityType);
            schema.Items.Add(cvlIdSimpletype);

            IEnumerable<XmlSchemaSimpleType> uniqueSimpleTypes = this.GetUniqueFieldsSimpleType(entityType);
            foreach (XmlSchemaSimpleType uniqueSimpleType in uniqueSimpleTypes)
            {
                schema.Items.Add(uniqueSimpleType);
            }

            List<string> cvlIds = new List<string>();
            foreach (FieldType fieldType in entityType.FieldTypes)
            {
                if (string.IsNullOrEmpty(fieldType.CVLId))
                {
                    continue;
                }

                if (cvlIds.Contains(fieldType.CVLId))
                {
                    continue;
                }

                cvlIds.Add(fieldType.CVLId);
            }

            foreach (string cvlId in cvlIds)
            {
                XmlSchemaSimpleType cvlSimpleType = this.GetCvlSimpleType(cvlId);

                if (cvlSimpleType == null)
                {
                    continue;
                }

                schema.Items.Add(cvlSimpleType);
            }

            return schema;
        }

        private XmlSchemaObject GetSpecificationDataRowTypes()
        {
            var cvlAttribute = new XmlSchemaAttribute
            {
                Name = "cvl",
                Use = XmlSchemaUse.Optional,
                SchemaTypeName = new XmlQualifiedName("string", W3Namespace)
            };
            var complexDataType = new XmlSchemaComplexType();

            var simpleContent = new XmlSchemaSimpleContent();
            complexDataType.ContentModel = simpleContent;

            var extension = new XmlSchemaSimpleContentExtension();
            simpleContent.Content = extension;
            extension.BaseTypeName = new XmlQualifiedName("string", W3Namespace);
            extension.Attributes.Add(cvlAttribute);

            complexDataType.Name = "StringType";

            return complexDataType;
        }

        private XmlSchemaComplexType GetEntitiesComplexType(EntityType entityType)
        {
            XmlSchemaChoice choice = new XmlSchemaChoice { MaxOccursString = "unbounded" };
            choice.Items.Add(new XmlSchemaElement
            {
                Name = entityType.Id,
                SchemaTypeName = new XmlQualifiedName(entityType.Id, InriverNamespace)
            });

            XmlSchemaComplexType complextType = new XmlSchemaComplexType { Name = string.Format("{0}s", entityType.Id), Particle = choice };
            return complextType;
        }

        private XmlSchemaComplexType GetEntityComplexType(EntityType entityType)
        {
            XmlSchemaSequence sequence = new XmlSchemaSequence { MaxOccurs = 1, MinOccurs = 0 };

            sequence.Items.Add(this.GetFieldElements(entityType.Id));
            sequence.Items.Add(this.GetLinkElements(entityType.Id));
            sequence.Items.Add(this.GetAdditionalElements(entityType.Id));

            XmlSchemaComplexType schemaType = new XmlSchemaComplexType { Name = entityType.Id, Particle = sequence };
            if (entityType.FieldSets.Any())
            {
                schemaType.Attributes.Add(new XmlSchemaAttribute { Name = "FieldSet", Use = XmlSchemaUse.Optional, SchemaTypeName = new XmlQualifiedName(string.Format("{0}FieldSets", entityType.Id), InriverNamespace) });
            }

            schemaType.Attributes.Add(new XmlSchemaAttribute { Name = "EntityId", Use = XmlSchemaUse.Optional, SchemaTypeName = new XmlQualifiedName("integer", W3Namespace) });
            schemaType.Attributes.Add(new XmlSchemaAttribute { Name = "Action", Use = XmlSchemaUse.Optional, SchemaTypeName = new XmlQualifiedName("EntityActions", InriverNamespace) });
            schemaType.Attributes.Add(new XmlSchemaAttribute { Name = "ExternalUniqueIdField", Use = XmlSchemaUse.Optional, SchemaTypeName = new XmlQualifiedName(string.Format("{0}ExternalUniqueIdFields", entityType.Id), InriverNamespace) });

            return schemaType;
        }

        private XmlSchemaComplexType GetFieldsComplexType(EntityType entityType)
        {
            XmlSchemaChoice choice = new XmlSchemaChoice { MaxOccursString = "unbounded" };

            foreach (FieldType fieldType in entityType.FieldTypes)
            {
                XmlSchemaElement element = this.CreateFieldElement(fieldType);
                element.MinOccurs = fieldType.Mandatory ? 1 : 0;
                choice.Items.Add(element);
            }

            return new XmlSchemaComplexType { Name = string.Format("{0}FieldsType", entityType.Id), Particle = choice };
        }

        private XmlSchemaComplexType GetLinksComplexType(string entityTypeId)
        {
            XmlSchemaSequence sequence = new XmlSchemaSequence();
            sequence.Items.Add(
               new XmlSchemaElement
               {
                   Name = entityTypeId + "ParentLinks",
                   SchemaTypeName = new XmlQualifiedName(entityTypeId + "ParentLinksType", InriverNamespace),
               });
            sequence.Items.Add(
               new XmlSchemaElement
               {
                   Name = entityTypeId + "ChildLinks",
                   SchemaTypeName = new XmlQualifiedName(entityTypeId + "ChildLinksType", InriverNamespace),
               });

            return new XmlSchemaComplexType { Name = entityTypeId + "LinksType", Particle = sequence };
        }

        private XmlSchemaComplexType GetSpecificationDataComplexType()
        {
            XmlSchemaChoice choice = new XmlSchemaChoice { MinOccurs = 0, MaxOccursString = "unbounded" };
            choice.Items.Add(new XmlSchemaElement { Name = "Row", MaxOccursString = "unbounded", SchemaTypeName = new XmlQualifiedName("SpecificationDataRowType", InriverNamespace) });

            return new XmlSchemaComplexType { Name = "SpecificationDataType", Particle = choice };
        }

        private XmlSchemaComplexType GetSpecificationDataRowComplexType()
        {
            // var cvlAttribute = new XmlSchemaAttribute
            // {
            //    Name = "cvl",
            //    Use = XmlSchemaUse.Optional,
            //    SchemaTypeName = new XmlQualifiedName("string", W3Namespace)
            // };
            // var complexDataType = new XmlSchemaComplexType();

            // var simpleContent = new XmlSchemaSimpleContent();
            // complexDataType.ContentModel = simpleContent;
            // var extension = new XmlSchemaSimpleContentExtension();
            // simpleContent.Content = extension;
            // extension.BaseTypeName = new XmlQualifiedName("string", W3Namespace);
            // extension.Attributes.Add(cvlAttribute);
            // var sequence = new XmlSchemaSequence { MinOccurs = 0, MaxOccursString = "unbounded" };
            // sequence.Items.Add(new XmlSchemaElement { Name = "SpecificationFieldTypeId", MinOccurs = 1, MaxOccurs = 1, SchemaTypeName = new XmlQualifiedName("string", W3Namespace) });
            // sequence.Items.Add(new XmlSchemaElement { Name = "Data", MinOccurs = 1, MaxOccursString = "unbounded", SchemaType = complexDataType });

            // return new XmlSchemaComplexType { Name = "SpecificationDataRowType", Particle = sequence };
            XmlSchemaSequence sequence = new XmlSchemaSequence { MinOccurs = 0, MaxOccursString = "unbounded" };
            sequence.Items.Add(new XmlSchemaElement { Name = "SpecificationFieldTypeId", MinOccurs = 1, MaxOccurs = 1, SchemaTypeName = new XmlQualifiedName("string", W3Namespace) });

            // sequence.Items.Add(new XmlSchemaElement { Name = "Data", MinOccurs = 1, MaxOccursString = "unbounded", SchemaTypeName = new XmlQualifiedName("string", W3Namespace) });
            sequence.Items.Add(new XmlSchemaElement { Name = "Data", MinOccurs = 1, MaxOccursString = "unbounded" });

            return new XmlSchemaComplexType { Name = "SpecificationDataRowType", Particle = sequence };
        }

        private XmlSchemaComplexType GetSpecificationTemplateComplexType()
        {
            XmlSchemaChoice choice = new XmlSchemaChoice { MinOccurs = 0, MaxOccursString = "unbounded" };
            choice.Items.Add(new XmlSchemaElement { Name = "SpecificationFieldType", MaxOccursString = "unbounded", SchemaTypeName = new XmlQualifiedName("SpecificationFieldTypeType", InriverNamespace) });

            return new XmlSchemaComplexType { Name = "SpecificationTemplateType", Particle = choice };
        }

        private XmlSchemaComplexType GetSpecificationFieldTypeCompleteType()
        {
            XmlSchemaSequence sequence = new XmlSchemaSequence { MinOccurs = 0, MaxOccursString = "unbounded" };
            sequence.Items.Add(new XmlSchemaElement { Name = "Id", MinOccurs = 1, MaxOccurs = 1, SchemaTypeName = new XmlQualifiedName("string", W3Namespace) });
            sequence.Items.Add(new XmlSchemaElement { Name = "Name", MinOccurs = 1, MaxOccurs = 1, SchemaTypeName = new XmlQualifiedName("LocaleStringType", InriverNamespace) });
            sequence.Items.Add(new XmlSchemaElement { Name = "DataType", MinOccurs = 1, MaxOccurs = 1, SchemaTypeName = new XmlQualifiedName("string", W3Namespace) });
            sequence.Items.Add(new XmlSchemaElement { Name = "Mandatory", MinOccurs = 1, MaxOccurs = 1, SchemaTypeName = new XmlQualifiedName("boolean", W3Namespace) });
            sequence.Items.Add(new XmlSchemaElement { Name = "Index", MinOccurs = 1, MaxOccurs = 1, SchemaTypeName = new XmlQualifiedName("integer", W3Namespace) });
            sequence.Items.Add(new XmlSchemaElement { Name = "CategoryId", MinOccurs = 1, MaxOccurs = 1, SchemaTypeName = new XmlQualifiedName("string", W3Namespace) });
            sequence.Items.Add(new XmlSchemaElement { Name = "CategoryName", MinOccurs = 1, MaxOccurs = 1, SchemaTypeName = new XmlQualifiedName("LocaleStringType", InriverNamespace) });
            sequence.Items.Add(new XmlSchemaElement { Name = "DefaultValue", MinOccurs = 1, MaxOccurs = 1, SchemaTypeName = new XmlQualifiedName("string", W3Namespace) });
            sequence.Items.Add(new XmlSchemaElement { Name = "CVLId", MinOccurs = 1, MaxOccurs = 1, SchemaTypeName = new XmlQualifiedName("string", W3Namespace) });
            sequence.Items.Add(new XmlSchemaElement { Name = "Multivalue", MinOccurs = 1, MaxOccurs = 1, SchemaTypeName = new XmlQualifiedName("boolean", W3Namespace) });
            sequence.Items.Add(new XmlSchemaElement { Name = "Unit", MinOccurs = 1, MaxOccurs = 1, SchemaTypeName = new XmlQualifiedName("string", W3Namespace) });
            sequence.Items.Add(new XmlSchemaElement { Name = "Additional", MinOccurs = 1, MaxOccurs = 1, SchemaTypeName = new XmlQualifiedName("string", W3Namespace) });

            return new XmlSchemaComplexType { Name = "SpecificationFieldTypeType", Particle = sequence };
        }

        private XmlSchemaComplexType GetSubLinksComplexType(EntityType entityType, bool parent)
        {
            XmlSchemaChoice choice = new XmlSchemaChoice { MinOccurs = 0, MaxOccursString = "unbounded" };
            List<LinkType> linkTypes = parent ? entityType.GetInboundLinkTypes() : entityType.GetOutboundLinkTypes();

            foreach (LinkType linkType in linkTypes)
            {
                if (choice.Items.Cast<XmlSchemaElement>()
                        .Any(p => p.Name == linkType.Id))
                {
                    continue;
                }

                string uniqueFieldNameType = string.Format("{0}UniqueFields", linkType.SourceEntityTypeId != entityType.Id ? linkType.SourceEntityTypeId : linkType.TargetEntityTypeId);
                XmlSchemaComplexType complexType = new XmlSchemaComplexType();
                XmlSchemaAttribute attribute = new XmlSchemaAttribute
                {
                    Name = "SourceEntityTypeId",
                    Use = XmlSchemaUse.Required,
                    FixedValue = linkType.SourceEntityTypeId,
                    SchemaTypeName = new XmlQualifiedName("string", W3Namespace)
                };

                complexType.Attributes.Add(attribute);
                attribute = new XmlSchemaAttribute
                {
                    Name = "TargetEntityTypeId",
                    Use = XmlSchemaUse.Required,
                    FixedValue = linkType.TargetEntityTypeId,
                    SchemaTypeName = new XmlQualifiedName("string", W3Namespace)
                };

                complexType.Attributes.Add(attribute);
                attribute = new XmlSchemaAttribute
                {
                    Name = "UniqueFieldName",
                    Use = XmlSchemaUse.Required,
                    SchemaTypeName = new XmlQualifiedName(uniqueFieldNameType, InriverNamespace)
                };
                complexType.Attributes.Add(attribute);
                attribute = new XmlSchemaAttribute
                {
                    Name = "UniqueValue",
                    Use = XmlSchemaUse.Required,
                    SchemaTypeName = new XmlQualifiedName("string", W3Namespace)
                };
                complexType.Attributes.Add(attribute);
                attribute = new XmlSchemaAttribute
                {
                    Name = parent ? "SourceEntityId" : "TargetEntityId",
                    Use = XmlSchemaUse.Required,
                    SchemaTypeName = new XmlQualifiedName("integer", W3Namespace)
                };
                complexType.Attributes.Add(attribute);
                attribute = new XmlSchemaAttribute
                {
                    Name = "LinkEntityId",
                    Use = XmlSchemaUse.Optional,
                    SchemaTypeName = new XmlQualifiedName("string", W3Namespace)
                };
                complexType.Attributes.Add(attribute);
                attribute = new XmlSchemaAttribute
                {
                    Name = "LinkEntityTypeId",
                    Use = XmlSchemaUse.Optional,
                    SchemaTypeName = new XmlQualifiedName("string", W3Namespace)
                };
                complexType.Attributes.Add(attribute);
                attribute = new XmlSchemaAttribute
                {
                    Name = "Action",
                    Use = XmlSchemaUse.Optional,
                    SchemaTypeName = new XmlQualifiedName("LinkActions", InriverNamespace)
                };
                complexType.Attributes.Add(attribute);
                attribute = new XmlSchemaAttribute
                {
                    Name = "SortOrder",
                    Use = XmlSchemaUse.Optional,
                    SchemaTypeName = new XmlQualifiedName("integer", W3Namespace)
                };
                complexType.Attributes.Add(attribute);

                choice.Items.Add(
                    new XmlSchemaElement
                    {
                        Name = linkType.Id,
                        SchemaType = complexType
                    });
            }

            string schemaName = parent
                                    ? string.Format("{0}ParentLinksType", entityType.Id)
                                    : string.Format("{0}ChildLinksType", entityType.Id);
            return new XmlSchemaComplexType { Name = schemaName, Particle = choice };
        }

        private XmlSchemaComplexType GetAdditionalComplexType(EntityType entityType)
        {
            XmlSchemaChoice choice = new XmlSchemaChoice { MinOccurs = 0, MaxOccursString = "unbounded" };
            choice.Items.Add(new XmlSchemaElement { Name = "SpecificationData", SchemaTypeName = new XmlQualifiedName("SpecificationDataType", InriverNamespace) });
            choice.Items.Add(new XmlSchemaElement { Name = "SpecificationTemplate", SchemaTypeName = new XmlQualifiedName("SpecificationTemplateType", InriverNamespace) });

            return new XmlSchemaComplexType { Name = string.Format("{0}AdditionalsType", entityType.Id), Particle = choice };
        }

        private IEnumerable<XmlSchemaSimpleType> GetUniqueFieldsSimpleType(EntityType entityType)
        {
            IList<XmlSchemaSimpleType> result = new List<XmlSchemaSimpleType>();

            foreach (LinkType linkType in entityType.LinkTypes)
            {
                EntityType otherEntityType = this.manager.ModelService.GetEntityType(linkType.SourceEntityTypeId != entityType.Id ? linkType.SourceEntityTypeId : linkType.TargetEntityTypeId);
                if (result.Any(p => p.Name == string.Format("{0}UniqueFields", otherEntityType.Id)))
                {
                    // Already exists.
                    // Probably an entity type linked to the same entity type.
                    continue;
                }

                XmlSchemaSimpleTypeRestriction restriction = new XmlSchemaSimpleTypeRestriction
                {
                    BaseTypeName =
                        new XmlQualifiedName("string", W3Namespace)
                };

                // If nothing is unique and possible there should always be the entity id to connect to.
                XmlSchemaEnumerationFacet enumerationFacet = new XmlSchemaEnumerationFacet { Value = "EntityId" };
                restriction.Facets.Add(enumerationFacet);

                foreach (FieldType fieldType in otherEntityType.FieldTypes)
                {
                    if (!fieldType.Unique)
                    {
                        continue;
                    }

                    enumerationFacet = new XmlSchemaEnumerationFacet { Value = fieldType.Id };
                    restriction.Facets.Add(enumerationFacet);
                }

                result.Add(new XmlSchemaSimpleType { Name = string.Format("{0}UniqueFields", otherEntityType.Id), Content = restriction });
            }

            return result;
        }

        private XmlSchemaObject GetLinkElements(string entityTypeId)
        {
            return new XmlSchemaElement { Name = string.Format("{0}Links", entityTypeId), SchemaTypeName = new XmlQualifiedName(string.Format("{0}LinksType", entityTypeId), InriverNamespace) };
        }

        private XmlSchemaObject GetAdditionalElements(string entityTypeId)
        {
            return new XmlSchemaElement { Name = string.Format("{0}Additionals", entityTypeId), SchemaTypeName = new XmlQualifiedName(string.Format("{0}AdditionalsType", entityTypeId), InriverNamespace) };
        }

        private XmlSchemaElement GetFieldElements(string entityTypeId)
        {
            return new XmlSchemaElement { Name = string.Format("{0}Fields", entityTypeId), SchemaTypeName = new XmlQualifiedName(string.Format("{0}FieldsType", entityTypeId), InriverNamespace) };
        }

        private XmlSchemaElement CreateFieldElement(FieldType fieldType)
        {
            XmlSchemaChoice innerChoice;
            if (fieldType.DataType == DataType.CVL && fieldType.Multivalue)
            {
                innerChoice = new XmlSchemaChoice { MaxOccursString = "unbounded" };
            }
            else
            {
                innerChoice = new XmlSchemaChoice();
            }

            innerChoice.Items.Add(new XmlSchemaElement { Name = "Data", SchemaTypeName = this.GetDataElementType(fieldType) });
            XmlSchemaComplexType complexType = new XmlSchemaComplexType { Particle = innerChoice };
            if (!string.IsNullOrEmpty(fieldType.CVLId))
            {
                XmlSchemaAttribute attribute = new XmlSchemaAttribute
                {
                    Name = "Cvl",
                    Use = XmlSchemaUse.Optional,
                    SchemaTypeName =
                                                           new XmlQualifiedName(
                                                           "CvlIds",
                                                           InriverNamespace)
                };
                complexType.Attributes.Add(attribute);
            }

            XmlSchemaElement element = new XmlSchemaElement { Name = fieldType.Id, SchemaType = complexType };
            return element;
        }

        private XmlQualifiedName GetDataElementType(FieldType fieldType)
        {
            XmlQualifiedName result;

            if (fieldType.DataType == "CVL")
            {
                result = new XmlQualifiedName(string.Format("{0}Cvl", fieldType.CVLId), InriverNamespace);
            }
            else if (fieldType.DataType == "LocaleString")
            {
                result = new XmlQualifiedName("LocaleStringType", InriverNamespace);
            }
            else
            {
                result = new XmlQualifiedName("string", W3Namespace);
            }

            return result;
        }

        private XmlSchemaSimpleType GetCvlSimpleType(string cvlId)
        {
            if (string.IsNullOrWhiteSpace(cvlId))
            {
                return null;
            }

            XmlSchemaSimpleTypeRestriction cvlRestristion = new XmlSchemaSimpleTypeRestriction { BaseTypeName = new XmlQualifiedName("string", W3Namespace) };

            // Allow empty as a value.
            var enumerationFacet = new XmlSchemaEnumerationFacet { Value = string.Empty };
            cvlRestristion.Facets.Add(enumerationFacet);

            IList<CVLValue> cvlValues = this.manager.ModelService.GetCVLValuesForCVL(cvlId);
            foreach (CVLValue cvlValue in cvlValues)
            {
                enumerationFacet = new XmlSchemaEnumerationFacet { Value = cvlValue.Key };
                cvlRestristion.Facets.Add(enumerationFacet);
            }

            return new XmlSchemaSimpleType { Name = string.Format("{0}Cvl", cvlId), Content = cvlRestristion };
        }

        private XmlSchemaSimpleType GetFieldSetSimpleType(EntityType entityType)
        {
            XmlSchemaSimpleTypeRestriction restriction = new XmlSchemaSimpleTypeRestriction
            {
                BaseTypeName =
                    new XmlQualifiedName("string", W3Namespace)
            };
            foreach (FieldSet fieldSet in entityType.FieldSets)
            {
                XmlSchemaEnumerationFacet enumerationFacet = new XmlSchemaEnumerationFacet { Value = fieldSet.Id };
                restriction.Facets.Add(enumerationFacet);
            }

            return new XmlSchemaSimpleType { Name = string.Format("{0}FieldSets", entityType.Id), Content = restriction };
        }

        private XmlSchemaSimpleType GetExternalUniqueIdFieldSimpleType(EntityType entityType)
        {
            XmlSchemaSimpleTypeRestriction restriction = new XmlSchemaSimpleTypeRestriction
            {
                BaseTypeName =
                    new XmlQualifiedName("string", W3Namespace)
            };

            foreach (FieldType type in entityType.FieldTypes)
            {
                if (type.Unique && type.Mandatory)
                {
                    restriction.Facets.Add(new XmlSchemaEnumerationFacet { Value = type.Id });
                }
            }

            return new XmlSchemaSimpleType { Name = string.Format("{0}ExternalUniqueIdFields", entityType.Id), Content = restriction };
        }

        private XmlSchemaSimpleType GetEntityActionSimpleType()
        {
            XmlSchemaSimpleTypeRestriction restriction = new XmlSchemaSimpleTypeRestriction
            {
                BaseTypeName =
                    new XmlQualifiedName("string", W3Namespace)
            };

            XmlSchemaEnumerationFacet enumerationFacet = new XmlSchemaEnumerationFacet { Value = "New" };
            restriction.Facets.Add(enumerationFacet);
            enumerationFacet = new XmlSchemaEnumerationFacet { Value = "Updated" };
            restriction.Facets.Add(enumerationFacet);
            enumerationFacet = new XmlSchemaEnumerationFacet { Value = "Deleted" };
            restriction.Facets.Add(enumerationFacet);

            return new XmlSchemaSimpleType { Name = "EntityActions", Content = restriction };
        }

        private XmlSchemaSimpleType GetLinkActionSimpleType()
        {
            XmlSchemaSimpleTypeRestriction restriction = new XmlSchemaSimpleTypeRestriction
            {
                BaseTypeName =
                    new XmlQualifiedName("string", W3Namespace)
            };

            XmlSchemaEnumerationFacet enumerationFacet = new XmlSchemaEnumerationFacet { Value = "Deleted" };
            restriction.Facets.Add(enumerationFacet);

            return new XmlSchemaSimpleType { Name = "LinkActions", Content = restriction };
        }

        private XmlSchemaSimpleType GetLanguageSimpleType()
        {
            IList<CultureInfo> languages = this.manager.UtilityService.GetAllLanguages();
            XmlSchemaSimpleTypeRestriction restriction = new XmlSchemaSimpleTypeRestriction
            {
                BaseTypeName =
                    new XmlQualifiedName(
                    "string",
                    W3Namespace)
            };
            foreach (CultureInfo ci in languages)
            {
                XmlSchemaEnumerationFacet enumerationFacet = new XmlSchemaEnumerationFacet { Value = ci.Name };
                restriction.Facets.Add(enumerationFacet);
            }

            return new XmlSchemaSimpleType { Name = "Language", Content = restriction };
        }

        private XmlSchemaComplexType GetLocalStringComplexType()
        {
            XmlSchemaSequence innerSequence = new XmlSchemaSequence();
            innerSequence.Items.Add(new XmlSchemaElement { Name = "Language", SchemaTypeName = new XmlQualifiedName("Language", InriverNamespace) });
            innerSequence.Items.Add(new XmlSchemaElement { Name = "Value", SchemaTypeName = new XmlQualifiedName("string", W3Namespace) });

            XmlSchemaComplexType innerComplexType = new XmlSchemaComplexType { Particle = innerSequence };
            XmlSchemaElement localstringElement = new XmlSchemaElement { Name = "LocaleString", SchemaType = innerComplexType };
            XmlSchemaSequence outerSequence = new XmlSchemaSequence { MinOccurs = 0, MaxOccursString = "unbounded" };
            outerSequence.Items.Add(localstringElement);

            return new XmlSchemaComplexType { Name = "LocaleStringType", Particle = outerSequence };
        }

        private XmlSchemaSimpleType GetCvlIdSimpleType(EntityType entityType)
        {
            XmlSchemaSimpleTypeRestriction restriction = new XmlSchemaSimpleTypeRestriction
            {
                BaseTypeName =
                    new XmlQualifiedName(
                    "string",
                    W3Namespace)
            };

            foreach (FieldType fieldType in entityType.FieldTypes)
            {
                if (string.IsNullOrEmpty(fieldType.CVLId))
                {
                    continue;
                }

                XmlSchemaEnumerationFacet enumerationFacet = new XmlSchemaEnumerationFacet { Value = fieldType.CVLId };
                restriction.Facets.Add(enumerationFacet);
            }

            return new XmlSchemaSimpleType { Name = "CvlIds", Content = restriction };
        }
    }
}
