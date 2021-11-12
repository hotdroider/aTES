using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using System.Collections.Generic;
using System.IO;

namespace aTES.Events.SchemaRegistry
{
    /// <summary>
    /// Dictionary of all supported schemas
    /// </summary>
    public class SchemaRepository
    {
        private Dictionary<SchemaDescriptor, JSchema> _schemas = new Dictionary<SchemaDescriptor, JSchema>();

        public SchemaRepository(string filePath)
        {
            InitSchemas(filePath);
        }

        private void InitSchemas(string filePath)
        {
            var resolver = new JSchemaUrlResolver();
            var jsons = Directory.GetFiles(filePath, "*.json", SearchOption.AllDirectories);

            foreach (var f in jsons)
            {
                var fl = new FileInfo(f);
                using var jsonFile = File.OpenText(f);
                using var reader = new JsonTextReader(jsonFile);

                var schema = JSchema.Load(reader, resolver);

                var key = new SchemaDescriptor()
                {
                    //ugly
                    Name = fl.Directory.FullName.Replace(Path.GetFullPath(filePath), string.Empty).Replace("\\", ".").Trim('.'),
                    Version = int.Parse(Path.GetFileNameWithoutExtension(f))
                };

                _schemas.Add(key, schema);
            }
        }

        public bool TryGetSchema(string name, int version, out JSchema schema)
        {
            var key = new SchemaDescriptor()
            {
                Name = name,
                Version = version
            };

            return _schemas.TryGetValue(key, out schema);
        }

        private struct SchemaDescriptor
        {
            public string Name { get; set; }
            public int Version { get; set; }
        }
    }
}
