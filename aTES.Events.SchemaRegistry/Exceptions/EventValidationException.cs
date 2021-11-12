using System;
using System.Collections.Generic;

namespace aTES.Events.SchemaRegistry
{
    /// <summary>
    /// Event validation exception
    /// </summary>
    public class EventValidationException : Exception
    {
        public EventValidationException()
            : base() { }

        public EventValidationException(string message)
            : base(message) { }

        public EventValidationException(string message, params string[] validationErrors)
            : base(message) 
        {
            ValidationErrors = validationErrors;
        }
        public EventValidationException(string message, IList<string> validationErrors)
            : base(message)
        {
            ValidationErrors = validationErrors;
        }

        public IList<string> ValidationErrors { get; private set; }

        public override string Message 
            => base.Message + Environment.NewLine + string.Join(Environment.NewLine, ValidationErrors);
    }
}
