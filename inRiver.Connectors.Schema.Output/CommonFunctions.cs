using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

using inRiver.Connectors.Schema.Output.Helpers;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;

namespace inRiver.Connectors.Schema.Output
{
    internal class CommonFunctions : ExtensionHelper, IExtensionHelper
    {
        private readonly MappingHelper mappingFileHelper;

        internal CommonFunctions(MappingHelper mappingFileHelper) 
            : base(mappingFileHelper)
        {
            this.mappingFileHelper = mappingFileHelper;
        }

        internal string GetUniqueId(Dictionary<string, string> externalUniqueFileTypes, int entityId, string entityType, XDocument entityDocument)
        {
            string result = entityId.ToString(CultureInfo.InvariantCulture);
            if (externalUniqueFileTypes.ContainsKey(entityType))
            {
                if (entityDocument.Root != null)
                {
                    XElement element = entityDocument.Root.Descendants().FirstOrDefault(n => externalUniqueFileTypes[entityType].Equals(n.Name.LocalName));
                    if (element != null)
                    {
                        XElement dataElement = element.Descendants().FirstOrDefault(nn => nn.Name.LocalName.Equals("Data"));
                        if (dataElement != null)
                        {
                            result = dataElement.Value;
                        }
                    }
                }
            }

            return result;
        }

        internal bool IsConnectChannelId(int channelId, Dictionary<string, string> adapterSettings)
        {
            int connectorChannelId;
            if (!adapterSettings.ContainsKey(ConnectorSettings.ChannelId) || !int.TryParse(adapterSettings[ConnectorSettings.ChannelId], out connectorChannelId))
            {
                Context.Log(LogLevel.Error,$"Error, when parsing channel id {channelId} against connector settings");
                return false;
            }

            if (connectorChannelId != channelId)
            {
                // Not interested call.
                return false;
            }

            return true;
        }

        internal int GetPercentage(int totalAmount, int totalCount)
        {
            double amount = totalAmount;
            double count = totalCount;
            double percentage = ((amount - count) / amount) * 100;
            int result = (int)percentage;
            if (result != 100)
            {
                // Raise the number to the above value to avoid 0 too much.
                result++;
            }

            return result;
        }

        internal bool ShouldEntityBeExported(int entityId, Dictionary<string, string> adapterSettings)
        {
            List<string> entityTypesToExport = mappingFileHelper.GetEntityTypeIdToExportFromXml(adapterSettings[ConnectorSettings.MappingXml]);
            Entity shouldExportEntity = Context.ExtensionManager.DataService.GetEntity(entityId, LoadLevel.Shallow);
            return entityTypesToExport.Contains(shouldExportEntity.EntityType.Id);
        }

        internal bool ShouldEntityBeExported(Entity entity, Dictionary<string, string> adapterSettings)
        {
            List<string> entityTypesToExport = mappingFileHelper.GetEntityTypeIdToExportFromXml(adapterSettings[ConnectorSettings.MappingXml]);
            return entityTypesToExport.Contains(entity.EntityType.Id);
        }
    }
}