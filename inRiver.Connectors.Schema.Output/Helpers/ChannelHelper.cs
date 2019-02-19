#region Generated Code
namespace inRiver.Connectors.Schema.Output.Helpers
{
#endregion

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using inRiver.Remoting.Extension;
    using inRiver.Remoting.Log;
    using inRiver.Remoting.Objects;

    internal class ChannelHelper : ExtensionHelper
    {
        internal ChannelHelper(inRiverContext context)
            : base(context)
        { }

        internal Dictionary<string, List<Entity>> GetEntitiesInChannelByEntityType(int channelId, List<EntityType> entityTypes)
        {
            Dictionary<string, List<Entity>> entitiesInStructure = new Dictionary<string, List<Entity>>();
            try
            {
                foreach (EntityType entityType in entityTypes)
                {
                    List<Entity> entities = Context.ExtensionManager.ChannelService.GetEntitiesForChannelAndEntityType(channelId, entityType.Id);
                    if (entities.Any() && !entitiesInStructure.Keys.Contains(entityType.Id))
                    {
                        entitiesInStructure.Add(entityType.Id, entities);
                        Context.Log(LogLevel.Debug, string.Format("Found {0} entities in structure of type {1}", entities.Count, entityType));
                    }
                }
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, string.Empty, ex);
            }

            return entitiesInStructure;
        }

        internal Dictionary<string, List<int>> GetEntityIdsInChannelByEntityType(int channelId)
        {
            List<string> entityTypeIds = Context.ExtensionManager.ChannelService.GetAllEntityTypesForChannel(channelId);
            List<string> linkEntityTypeIds = Context.ExtensionManager.ChannelService.GetAllLinkEntityTypesForChannel(channelId);

            Dictionary<string, List<int>> entitiesInStructure = new Dictionary<string, List<int>>();
            try
            {
                foreach (string entityTypeId in entityTypeIds)
                {
                    List<Entity> entities = Context.ExtensionManager.ChannelService.GetEntitiesForChannelAndEntityType(channelId, entityTypeId);
                    List<int> ids = entities.Select(entity => entity.Id).ToList();
                    if (ids.Any() && !entitiesInStructure.Keys.Contains(entityTypeId))
                    {
                        entitiesInStructure.Add(entityTypeId, ids);
                        Context.Log(LogLevel.Debug, string.Format("Found {0} entities in structure of entity type {1}", ids.Count, entityTypeId));
                    }
                }

                foreach (string linkEntityTypeId in linkEntityTypeIds)
                {
                    List<Entity> entities = Context.ExtensionManager.ChannelService.GetEntitiesForChannelAndLinkEntityType(channelId, linkEntityTypeId);
                    List<int> ids = entities.Select(entity => entity.Id).ToList();
                    if (ids.Any() && !entitiesInStructure.Keys.Contains(linkEntityTypeId))
                    {
                        entitiesInStructure.Add(linkEntityTypeId, ids);
                        Context.Log(LogLevel.Debug, string.Format("Found {0} entities in structure of link entity type {1}", ids.Count, linkEntityTypeId));
                    }
                }
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, string.Empty, ex);
            }

            return entitiesInStructure;
        }
    }
}