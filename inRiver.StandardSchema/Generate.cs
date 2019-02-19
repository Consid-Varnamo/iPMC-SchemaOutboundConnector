using System.Xml.Schema;
using inRiver.Remoting;
using inRiver.Remoting.Objects;

namespace inRiver.SchemaBased
{
    public class Generate
    {
        private readonly object schemaRegeneratLock = new object();

        private readonly IinRiverManager manager;

        public Generate(IinRiverManager manager)
        {
            this.manager = manager;
        }

        public XmlSchema GenerateSchemaForEntityType(string entityTypeId)
        {
            lock (this.schemaRegeneratLock)
            {
                const LoadLevel LoadLevel = LoadLevel.DataAndLinks;
                EntityType entityType = this.manager.ModelService.GetEntityType(entityTypeId);
                var standardSchema = new StandardSchema(this.manager);
                XmlSchema schema = standardSchema.GenerateSchemaFromEntity(entityType, LoadLevel);
                return schema;
            }
        }
    }
}