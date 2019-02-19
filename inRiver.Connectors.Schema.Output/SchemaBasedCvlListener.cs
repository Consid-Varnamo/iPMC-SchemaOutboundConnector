using System;
using System.Collections.Generic;
using System.Reflection;
using inRiver.Remoting.Extension.Interface;

namespace inRiver.Connectors.Schema.Output
{
    public class SchemaBasedCvlListener : SchemaBasedListenerBase, ICVLListener
    {
        protected override List<string> additionalSettingsToValidate()
        {
            return null;
        }

        public void CVLValueCreated(string cvlId, string cvlValueKey)
        {
            if (!initializeListener())
                return;
            regenerateSchemaAndWriteCvl(cvlId, "NewValue");
        }

        public void CVLValueDeleted(string cvlId, string cvlValueKey)
        {
            if (!initializeListener())
                return;
            regenerateSchemaAndWriteCvl(cvlId, "DeletedValue");
        }

        public void CVLValueDeletedAll(string cvlId)
        {
            if (!initializeListener())
                return;
            regenerateSchemaAndWriteCvl(cvlId, "DeletedAll");
        }

        public void CVLValueUpdated(string cvlId, string cvlValueKey)
        {
            if (!initializeListener())
                return;
            regenerateSchemaAndWriteCvl(cvlId, "Updated");
        }

        void regenerateSchemaAndWriteCvl(string cvlId, string cvlAction)
        {
            var entityTypesToExport = mappingHelper.GetEntityTypeIdToExportFromXml(adapterSettings[ConnectorSettings.MappingXml]);
            schemaBasedOutput.ReGenerateSchema(entityTypesToExport);
            azureListenerHelper.WriteCvlById(cvlId, cvlAction, adapterSettings[ConnectorSettings.CvlFolder]);
        }

        public string Test()
        {
            string information = String.Format("{0}, ver: {1}", this.GetType().FullName, Assembly.GetExecutingAssembly().GetName().Version.ToString());
            return information;
        }

    }
}
