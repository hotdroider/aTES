using System;

namespace aTES.Events.SchemaRegistry.Exceptions
{
    public class SchemaNotFoundException : Exception
    {
        private const string MESSAGE = "Schema not found";

        public SchemaNotFoundException()
            : base(MESSAGE) { }

        public SchemaNotFoundException(string message)
            : base(message) { }
    }
}
