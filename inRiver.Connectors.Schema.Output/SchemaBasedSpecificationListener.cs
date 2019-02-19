using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using inRiver.Remoting.Extension;
using inRiver.Remoting.Extension.Interface;
using inRiver.Remoting.Objects;

namespace inRiver.Connectors.Schema.Output
{
    public class SchemaBasedSpecificationListener : SchemaBasedListenerBase, ISpecificationListener
    {

        /// <summary>
        /// Passes additional settings to baes class to insure they exist
        /// </summary>
        /// <returns></returns>
        protected override List<string> additionalSettingsToValidate()
        {
            return null;
        }

        public void SpecificationCategoryCreated(string categoryId)
        {
        }

        public void SpecificationCategoryDeleted(string categoryId)
        {
        }

        public void SpecificationCategoryUpdated(string categoryId)
        {
            if (!initializeListener())
                return;
            List<SpecificationFieldType> allSpecificationFieldTypes = Context.ExtensionManager.DataService.GetAllSpecificationFieldTypesForCategory(categoryId);
            List<int> potentialEntityIds = allSpecificationFieldTypes.Select(e => e.EntityId).Distinct().ToList();
            foreach(int entityId in potentialEntityIds)
            {
                updateChannelEntityId(entityId);
            }
        }

        public void SpecificationTemplateCreated(string templateId)
        {
            if (!initializeListener())
                return;
            updateChannelEntityForTemplate(templateId);
        }

        public void SpecificationTemplateDeleted(string templateId)
        {
        }

        public void SpecificationTemplateUpdated(string templateId)
        {
            if (!initializeListener())
                return;
            updateChannelEntityForTemplate(templateId);
        }

        public string Test()
        {
            string information = String.Format("{0}, ver: {1}", this.GetType().FullName, Assembly.GetExecutingAssembly().GetName().Version.ToString());
            return information;
        }
        void updateChannelEntityForTemplate(string templateId)
        {
            if (!initializeListener())
                return;
            SpecificationFieldType specificationFieldType = Context.ExtensionManager.DataService.GetSpecificationFieldType(templateId);
            updateChannelEntityId(specificationFieldType.EntityId);
        }

        void updateChannelEntityId(int entityId)
        {
            if (!initializeListener())
                return;
            int connectorChannelId;
            if (!adapterSettings.ContainsKey(ConnectorSettings.ChannelId) || !int.TryParse(adapterSettings[ConnectorSettings.ChannelId], out connectorChannelId))
            {
                return;
            }

            if (!Context.ExtensionManager.ChannelService.EntityExistsInChannel(connectorChannelId, entityId))
            {
                return;
            }

            SchemaBasedChannelListener channelListener = new SchemaBasedChannelListener(Context);
            channelListener.ChannelEntityUpdated(connectorChannelId, entityId, string.Empty);
        }


    }
}
