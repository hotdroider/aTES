using aTES.Common;
using aTES.Events.SchemaRegistry.Exceptions;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.Collections.Generic;
using System.Text.Json;

namespace aTES.Events.SchemaRegistry
{
    /// <summary>
    /// JSON schema registry to control event messages
    /// </summary>
    public class SchemaRegistry
    {
        private readonly SchemaRepository _schemaRepository;

        public SchemaRegistry(SchemaRepository schemaRepository)
        {
            _schemaRepository = schemaRepository;
        }

        /// <summary>
        /// Is event correct for some schema
        /// </summary>
        public bool ValidateEvent(Event validateMe, string eventType, int version)
        {
            var schema = GetSchema(eventType, version);

            var serializedToJson = JsonSerializer.Serialize(validateMe);
            var obj = JObject.Parse(serializedToJson);

            return obj.IsValid(schema);
        }

        /// <summary>
        /// Throw an error if event is not correct for some schema
        /// </summary>
        public void ThrowIfValidationFails(Event validateMe, string eventType, int version)
        {
            var schema = GetSchema(eventType, version);

            var msg = JsonSerializer.Serialize(validateMe);
            var obj = JObject.Parse(msg);

            if (!obj.IsValid(schema, out IList<string> errors))
                throw new EventValidationException($"Validation failed for schema {eventType} version {version}", errors);
        }

        private JSchema GetSchema(string messageType, int version)
        {
            if (!_schemaRepository.TryGetSchema(messageType, version, out var schema))
                throw new SchemaNotFoundException($"Schema for event {messageType} version {version} not found");

            return schema;
        }
    }
}
