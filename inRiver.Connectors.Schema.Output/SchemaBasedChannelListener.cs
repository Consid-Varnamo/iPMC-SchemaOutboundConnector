using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using inRiver.Remoting.Extension;
using inRiver.Remoting.Extension.Interface;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using inRiver.SchemaBased;

namespace inRiver.Connectors.Schema.Output
{
    public class SchemaBasedChannelListener : SchemaBasedListenerBase, IChannelListener
    {
        public SchemaBasedChannelListener()
        {
        }

        public SchemaBasedChannelListener(inRiverContext context)
        {
            this.Context = context;
            initializeListener();
        }

        private List<int> entitiyIdsAlreadywrittenToFile;

        public Dictionary<string, string> ExternalUniqueFileTypes
        {
            get
            {
                return mappingHelper.GetExternalUniqueFieldTypesFromXml(adapterSettings[ConnectorSettings.MappingXml]);
            }
        }

        public int MaxEntitiesInFile
        {
            get
            {
                int maxEntitiesInFile;
                if (!int.TryParse(adapterSettings[ConnectorSettings.MaxEntitiesInPublishedFile], out maxEntitiesInFile))
                {
                    maxEntitiesInFile = 0;
                }
                return maxEntitiesInFile;
            }
        }

        public bool SingleFile
        {
            get
            {
                bool singleFile;
                if (!bool.TryParse(adapterSettings[ConnectorSettings.PublishAsSingleFile], out singleFile))
                {
                    singleFile = true;
                }
                return singleFile;
            }
        }

        /// <summary>
        /// Passes additional settings to baes class to insure they exist
        /// </summary>
        /// <returns></returns>
        protected override List<string> additionalSettingsToValidate()
        {
            return new List<string>() { ConnectorSettings.ChannelId };
        }

        /// <summary>
        /// Validates whether the configured channel in settings matches the parameter
        /// </summary>
        bool isForConfiguredChannel(int channelId)
        {
            int configuredChannelId; 
            if (int.TryParse(adapterSettings?[ConnectorSettings.ChannelId], out configuredChannelId))
            {
                return (configuredChannelId == channelId);
            }
            return false;
        }

        public void AssortmentCopiedInChannel(int channelId, int assortmentId, int targetId, string targetType)
        {
        }

        public void ChannelEntityAdded(int channelId, int entityId)
        {
            if (!initializeListener())
                return;
            if (!isForConfiguredChannel(channelId))
            {
                return;
            }

            try
            {
                Entity entity = Context.ExtensionManager.DataService.GetEntity(entityId, LoadLevel.DataOnly);
                if (!WriteEntityToFile(channelId, entity, SchemabasedEntityActionEnum.New))
                {
                    return;
                }

                if (!adapterSettings.ContainsKey(ConnectorSettings.UpdateParentWhenAddEntity) ||
                    !string.Equals(adapterSettings[ConnectorSettings.UpdateParentWhenAddEntity], "False", StringComparison.InvariantCultureIgnoreCase))
                {
                    var entityLinkTypes = Context.ExtensionManager.ModelService.GetLinkTypesForEntityType(entity.EntityType.Id);
                    var updatedEntities = new List<Entity>();
                    foreach (var linkType in entityLinkTypes)
                    {
                        var links = Context.ExtensionManager.ChannelService.GetInboundLinksForEntityAndLinkType(channelId, entityId, linkType.Id);
                        updatedEntities.AddRange((from link in links select link.Source).ToList());
                    }

                    foreach (var updatedEntity in updatedEntities)
                    {
                        WriteEntityToFile(channelId, updatedEntity.Id, SchemabasedEntityActionEnum.Updated);
                    }
                }

                if (entity.EntityType.Id == "Resource")
                {
                    azureListenerHelper.StoreResourceFiles(new List<int> { entityId }, adapterSettings);
                }

                entitiyIdsAlreadywrittenToFile = new List<int>();
                GenerateChildEntityXml(entity, channelId);
            }
            catch (Exception exception)
            {
                Context.Log(LogLevel.Error, $"Error during ChannelEntityAdded Channel {channelId}, entity {entityId} ", exception);
            }
        }

        public void ChannelEntityDeleted(int channelId, Entity deletedEntity)
        {
            if (!initializeListener())
                return;
            if (!isForConfiguredChannel(channelId))
            {
                return;
            }

            Context.Log(LogLevel.Information, $"Entity with id {deletedEntity.Id} deleted in channel {channelId}");
            try
            {
                if (!WriteEntityToFile(channelId, deletedEntity, SchemabasedEntityActionEnum.Deleted))
                {
                    return;
                }
                Context.Log(LogLevel.Verbose, "Done");
            }
            catch (Exception exception)
            {
                Context.Log(LogLevel.Error, $"Error during ChannelEntityDeleted for Channel {channelId} entity {deletedEntity.Id}", exception);
            }
        }

        public void ChannelEntityFieldSetUpdated(int channelId, int entityId, string fieldSetId)
        {
            if (!initializeListener())
                return;
            if (!isForConfiguredChannel(channelId))
            {
                return;
            }

            Context.Log(LogLevel.Information, $"Entity with id {entityId} got fieldset {fieldSetId} updated in channel {channelId}");

            int exportingId = entityId;
            if (!common.ShouldEntityBeExported(exportingId, adapterSettings))
            {
                Entity notExporting = Context.ExtensionManager.DataService.GetEntity(exportingId, LoadLevel.Shallow);
                Context.Log(LogLevel.Information, $"Entity type {notExporting.EntityType.Id} are not included in the list that shall be exported.");
                return;
            }

            ChannelEntityUpdated(channelId, entityId, string.Empty);
            Context.Log(LogLevel.Verbose, "Done");
        }

        public void ChannelEntitySpecificationFieldAdded(int channelId, int entityId, string fieldName)
        {
            if (!initializeListener())
                return;
            if (!isForConfiguredChannel(channelId))
            {
                return;
            }

            Context.Log(LogLevel.Information, $"ChannelEntitySpecificationFieldAdded called channel {channelId}, entity {entityId}, field {fieldName}");
            ChannelEntityUpdated(channelId, entityId, string.Empty);
        }

        public void ChannelEntitySpecificationFieldUpdated(int channelId, int entityId, string fieldName)
        {
            if (!initializeListener())
                return;
            if (!isForConfiguredChannel(channelId))
            {
                return;
            }

            Context.Log(LogLevel.Information, $"ChannelEntitySpecificationFieldUpdated called channel {channelId}, entity {entityId}, field {fieldName}");
            ChannelEntityUpdated(channelId, entityId, string.Empty);
        }

        public void ChannelEntityUpdated(int channelId, int entityId, string data)
        {
            if (!initializeListener())
                return;
            if (!isForConfiguredChannel(channelId))
            {
                return;
            }

            Context.Log(LogLevel.Information, $"Entity with {entityId} updated in channel {channelId}");
            try
            {
                Entity entity = Context.ExtensionManager.DataService.GetEntity(entityId, LoadLevel.DataOnly);
                WriteEntityToFile(channelId, entity, SchemabasedEntityActionEnum.Updated);

                if (entity.EntityType.Id == "Resource")
                {
                    azureListenerHelper.StoreResourceFiles(new List<int> { entityId }, adapterSettings);
                }
                Context.Log(LogLevel.Verbose, "Done");
            }
            catch (Exception exception)
            {
                Context.Log(LogLevel.Error, $"Error during ChannelEntityUpdated channel {channelId} entity {entityId}", exception);
            }
        }

        public void ChannelLinkAdded(int channelId, int sourceEntityId, int targetEntityId, string linkTypeId, int? linkEntityId)
        {
            if (!initializeListener())
                return;
            if (!isForConfiguredChannel(channelId))
            {
                return;
            }

            Context.Log(LogLevel.Information, $"Link between {sourceEntityId} and {targetEntityId} added to channel {channelId}");
            try
            {
                if (!Context.ExtensionManager.ChannelService.EntityExistsInChannel(channelId, targetEntityId))
                {
                    var msg = string.Format("Entity with id {0} does not exist in channel. Aborting action.", targetEntityId);
                    Context.Log(LogLevel.Error, msg);
                    return;
                }

                if (!adapterSettings.ContainsKey(ConnectorSettings.UpdateParentWhenAddEntity)
                    || !string.Equals(adapterSettings[ConnectorSettings.UpdateParentWhenAddEntity], "False", StringComparison.InvariantCultureIgnoreCase))
                {
                    // Handling source (parent) entity
                    WriteEntityToFile(channelId, sourceEntityId, SchemabasedEntityActionEnum.Updated);
                }

                var targetEntityAction = SchemabasedEntityActionEnum.New;
                if (Context.ExtensionManager.ChannelService.GetAllStructureEntitiesForEntityInChannel(channelId, targetEntityId).Count > 1)
                {
                    targetEntityAction = SchemabasedEntityActionEnum.Updated;
                }

                // Handling target (child) entity
                var targetEntity = Context.ExtensionManager.DataService.GetEntity(targetEntityId, LoadLevel.DataOnly);
                WriteEntityToFile(channelId, targetEntity, targetEntityAction);

                if (targetEntity.EntityType.Id == "Resource")
                {
                    azureListenerHelper.StoreResourceFiles(new List<int> { targetEntityId }, this.adapterSettings);
                }

                if (targetEntityAction == SchemabasedEntityActionEnum.New)
                {
                    this.entitiyIdsAlreadywrittenToFile = new List<int>();
                    if (linkEntityId.HasValue)
                    {
                        if (WriteEntityToFile(channelId, linkEntityId.Value, targetEntityAction))
                        {
                            entitiyIdsAlreadywrittenToFile.Add(linkEntityId.Value);
                        }
                    }

                    GenerateChildEntityXml(targetEntity, channelId);
                }
                Context.Log(LogLevel.Verbose, "Done");
            }
            catch (Exception exception)
            {
                Context.Log(LogLevel.Error, $"Error during ChannelLinkAdded channel {channelId} between {sourceEntityId} and {targetEntityId}", exception);
            }

        }

        public void ChannelLinkDeleted(int channelId, int sourceEntityId, int targetEntityId, string linkTypeId, int? linkEntityId)
        {
            if (!initializeListener())
                return;
            if (!isForConfiguredChannel(channelId))
            {
                return;
            }

            Context.Log(LogLevel.Information, $"Link between {sourceEntityId} and {targetEntityId} deleted to channel {channelId}");
            try
            {
                Link linkToBeDeleted = new Link
                {
                    Source = Context.ExtensionManager.DataService.GetEntity(sourceEntityId, LoadLevel.DataAndLinks),
                    Target = Context.ExtensionManager.DataService.GetEntity(targetEntityId, LoadLevel.DataAndLinks),
                    LinkType = Context.ExtensionManager.ModelService.GetLinkType(linkTypeId),
                    LinkEntity = linkEntityId.HasValue ? Context.ExtensionManager.DataService.GetEntity(linkEntityId.Value, LoadLevel.DataOnly) : null
                };

                WriteEntityToFile(channelId, sourceEntityId, SchemabasedEntityActionEnum.Updated, linkToBeDeleted);
                var targetEntityAction = SchemabasedEntityActionEnum.Updated;
                if (!Context.ExtensionManager.ChannelService.EntityExistsInChannel(channelId, targetEntityId))
                {
                    targetEntityAction = SchemabasedEntityActionEnum.Deleted;
                }

                WriteEntityToFile(channelId, targetEntityId, targetEntityAction);
                Context.Log(LogLevel.Verbose, "Done");
            }
            catch (Exception exception)
            {
                Context.Log(LogLevel.Error, $"Error during ChannelLinkDeleted: channel {channelId}, source {sourceEntityId}, target {targetEntityId} ", exception);
            }

        }

        public void ChannelLinkUpdated(int channelId, int sourceEntityId, int targetEntityId, string linkTypeId, int? linkEntityId)
        {
            if (!initializeListener())
                return;
            if (!isForConfiguredChannel(channelId))
            {
                return;
            }

            Context.Log(LogLevel.Information, $"Link between {sourceEntityId} and {targetEntityId} updated to channel {channelId}");
            try
            {
                // Handling source (parent) entity
                WriteEntityToFile(channelId, sourceEntityId, SchemabasedEntityActionEnum.Updated);

                // Handling target (child) entity
                WriteEntityToFile(channelId, targetEntityId, SchemabasedEntityActionEnum.Updated);

                Context.Log(LogLevel.Verbose, "Done");
            }
            catch (Exception exception)
            {
                Context.Log(LogLevel.Error, $"Error during ChannelLinkUpdated between {sourceEntityId} and {targetEntityId} updated to channel {channelId} ", exception);
            }
        }

        private void GenerateChildEntityXml(Entity entity, int channelId, bool delete = false)
        {
            foreach (Link link in Context.ExtensionManager.ChannelService.GetOutboundLinksForEntity(channelId, entity.Id))
            {
                if (this.entitiyIdsAlreadywrittenToFile.Contains(link.Target.Id))
                {
                    continue;
                }

                if (Context.ExtensionManager.ChannelService.GetOutboundLinksForEntity(channelId, link.Target.Id).Count > 0)
                {
                    GenerateChildEntityXml(link.Target, channelId, delete);
                }

                var entityAction = SchemabasedEntityActionEnum.New;
                int entityId = link.Target.Id;
                if (delete)
                {
                    entityAction = !Context.ExtensionManager.ChannelService.EntityExistsInChannel(channelId, entityId)
                                       ? SchemabasedEntityActionEnum.Deleted
                                       : SchemabasedEntityActionEnum.Updated;
                }
                else
                {
                    if (Context.ExtensionManager.ChannelService.GetAllStructureEntitiesForEntityInChannel(channelId, entityId).Count > 1)
                    {
                        entityAction = SchemabasedEntityActionEnum.Updated;
                    }
                }

                Entity creating = Context.ExtensionManager.DataService.GetEntity(entityId, LoadLevel.DataOnly);

                if (WriteEntityToFile(channelId, creating, entityAction))
                {
                    this.entitiyIdsAlreadywrittenToFile.Add(creating.Id);
                }
                else
                {
                    Context.Log(LogLevel.Error, $"Error while generating xml for entity {entity.Id}");
                    return;
                }

                //Check link entity
                if (link.LinkEntity != null && !this.entitiyIdsAlreadywrittenToFile.Contains(link.LinkEntity.Id))
                {
                    creating = Context.ExtensionManager.DataService.GetEntity(link.LinkEntity.Id, LoadLevel.DataOnly);
                    if (WriteEntityToFile(channelId, creating, entityAction))
                    {
                        this.entitiyIdsAlreadywrittenToFile.Add(creating.Id);
                    }
                    else
                    {
                        Context.Log(LogLevel.Error, $"Error while generating xml for entity {entity.Id}");
                        return;
                    }
                }

                if (creating.EntityType.Id == "Resource")
                {
                    azureListenerHelper.StoreResourceFiles(new List<int> { entityId }, adapterSettings);
                }
            }
        }

        public void Publish(int channelId)
        {
            if (!initializeListener())
                return;

            if (!isForConfiguredChannel(channelId))
            {
                return;
            }

            bool shouldGenerateSchema = true;
            const SchemabasedEntityActionEnum SchemabasedEntityAction = SchemabasedEntityActionEnum.New;
            int percentage = 0;

            //const ConnectorEventType CurrentEventType = ConnectorEventType.Publish;
            //Guid sessionId = EventHelper.FireEvent(channelId, this.Id, CurrentEventType, 0, string.Format("Publish started for channel id {0}", channelId));
            Context.Log(LogLevel.Information, $"Publish started for channel id {channelId}");
            try
            {
                Dictionary<string, List<int>> entitiesInStructure = channelHelper.GetEntityIdsInChannelByEntityType(channelId);
                int totalEntities = entitiesInStructure.Aggregate(0, (current, pair) => current + pair.Value.Count);
                int totalFiles = entitiesInStructure.Where(pair => pair.Key == "Resource")
                    .Sum(pair => pair.Value.Select(resource => Context.ExtensionManager.DataService.GetEntity(resource, LoadLevel.DataOnly))
                        .Select(entityWithField => entityWithField.GetField("ResourceFileId"))
                        .Count(identityField => identityField != null && identityField.Data != null));

                int totalAmount = totalEntities + totalFiles;
                int totalCount = totalAmount;

                List<string> entityTypesToExport = mappingHelper.GetEntityTypeIdToExportFromXml(adapterSettings[ConnectorSettings.MappingXml]);
                foreach (KeyValuePair<string, List<int>> valuePair in entitiesInStructure)
                {
                    percentage = common.GetPercentage(totalAmount, totalCount);
                    Context.Log(LogLevel.Verbose, $"Start working with {valuePair.Key}");
                    totalCount = totalCount - valuePair.Value.Count;
                    if (!entityTypesToExport.Contains(valuePair.Key))
                    {
                        Context.Log(LogLevel.Information, $"Entity type {valuePair.Key} is not included in the list that shall be exported.");
                        continue;
                    }

                    if (SingleFile)
                    {
                        if (MaxEntitiesInFile == 0)
                        {
                            // One big file per entity type
                            string entityTypeXml;
                            try
                            {
                                entityTypeXml = schemaBasedOutput.GenerateXml(valuePair, SchemabasedEntityAction, ExternalUniqueFileTypes, shouldGenerateSchema, channelId);
                            }
                            catch (Exception exception)
                            {
                                Context.Log(LogLevel.Error, $"Error while generating XML for {valuePair.Key}s:", exception);
                                continue;
                            }

                            XDocument doc = XDocument.Parse(entityTypeXml);
                            azureListenerHelper.PublishEntityTypeXml(valuePair.Key, doc, adapterSettings[ConnectorSettings.PublishFolder]);
                            azureListenerHelper.WriteCvlForEntityType(Context.ExtensionManager.ModelService.GetEntityType(valuePair.Key), adapterSettings[ConnectorSettings.CvlFolder]);
                        }
                        else
                        {
                            // Split it up into several files.
                            int refCount = 0;
                            List<int> entities = valuePair.Value;
                            int maxRange = entities.Count;
                            int fileNumber = 0;
                            while (refCount < maxRange)
                            {
                                int numberOfEntities = MaxEntitiesInFile;
                                if (entities.Count < refCount + numberOfEntities)
                                {
                                    numberOfEntities = entities.Count - refCount;
                                }

                                List<int> result = entities.GetRange(refCount, numberOfEntities);
                                if (result.Count == 0)
                                {
                                    break;
                                }

                                refCount += result.Count;
                                KeyValuePair<string, List<int>> entityTypePair = new KeyValuePair<string, List<int>>(valuePair.Key, result);
                                string entityTypeXml;
                                try
                                {
                                    entityTypeXml = schemaBasedOutput.GenerateXml(entityTypePair, SchemabasedEntityAction, ExternalUniqueFileTypes, shouldGenerateSchema, channelId);
                                    shouldGenerateSchema = false;
                                }
                                catch (Exception exception)
                                {
                                    Context.Log(LogLevel.Error, $"Error while generating XML for {valuePair.Key}s:", exception);
                                    continue;
                                }

                                XDocument doc = XDocument.Parse(entityTypeXml);
                                fileNumber++;
                                azureListenerHelper.PublishEntityTypeXml(valuePair.Key, doc, adapterSettings[ConnectorSettings.PublishFolder], fileNumber);
                            }
                            azureListenerHelper.WriteCvlForEntityType(Context.ExtensionManager.ModelService.GetEntityType(valuePair.Key), adapterSettings[ConnectorSettings.CvlFolder]);
                        }
                    }
                    else
                    {
                        // One file per entity
                        foreach (int entityId in valuePair.Value)
                        {
                            // string entityXml;
                            try
                            {
                                WriteEntityToFile(channelId, entityId, SchemabasedEntityAction);
                                shouldGenerateSchema = false;
                            }
                            catch (Exception exception)
                            {
                                Context.Log(LogLevel.Error, $"Error while generating XML for Entity with id {entityId}", exception);
                            }
                        }

                        EntityType entityType = Context.ExtensionManager.ModelService.GetEntityType(valuePair.Key);
                        azureListenerHelper.WriteCvlForEntityType(entityType, adapterSettings[ConnectorSettings.CvlFolder]);
                    }

                    if (valuePair.Key == "Resource")
                    {
                        percentage = common.GetPercentage(totalAmount, totalCount);
                        Context.Log(LogLevel.Verbose, "Start working with resource files");
                        totalCount = totalCount - azureListenerHelper.StoreResourceFiles(valuePair.Value, adapterSettings);
                    }
                }
                Context.Log(LogLevel.Information, "Publish complete.");
            }
            catch (Exception exception)
            {
                Context.Log(LogLevel.Error, $"Error when publishing channel {channelId} ", exception);
            }

        }

        public void Synchronize(int channelId)
        {
        }

        public string Test()
        {
            string information = String.Format("{0}, ver: {1}", this.GetType().FullName, Assembly.GetExecutingAssembly().GetName().Version.ToString());
            return information;
        }

        public void UnPublish(int channelId)
        {
        }

        private bool WriteEntityToFile(int channelId, int entityId, SchemabasedEntityActionEnum action, Link linkToBeDeleted = null)
        {
            var entity = Context.ExtensionManager.DataService.GetEntity(entityId, LoadLevel.DataOnly);
            return WriteEntityToFile(channelId, entity, action, linkToBeDeleted);
        }

        private bool WriteEntityToFile(int channelId, Entity entity, SchemabasedEntityActionEnum action, Link linkToBeDeleted = null)
        {
            if (entity == null)
            {
                return false;
            }

            if (!common.ShouldEntityBeExported(entity, adapterSettings))
            {
                Context.Log(LogLevel.Information, $"Entity type {entity.EntityType.Id} are not included in the list that shall be exported.");
                return false;
            }

            XDocument entityXml = schemaBasedOutput.GenerateXml(entity, action, ExternalUniqueFileTypes, channelId, linkToBeDeleted);
            if (!schemaBasedOutput.IsValid(entity.EntityType.Id, entityXml))
            {
                return false;
            }
            string uniqueId = common.GetUniqueId(ExternalUniqueFileTypes, entity.Id, entity.EntityType.Id, entityXml);
            return azureListenerHelper.WriteEntityXmlToBlobFile(entity.EntityType.Id, uniqueId, entityXml, this.adapterSettings[ConnectorSettings.PublishFolder]);
        }
    }
}
