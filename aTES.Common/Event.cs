using System;

namespace aTES.Common
{
    /// <summary>
    /// Common MB event
    /// </summary>
    public class Event<T> where T : class
    {
        public Event()
        {
            Id = Guid.NewGuid().ToString();
            Time = DateTime.UtcNow;
        }

        public static Event<T> CreateFrom(T source, string eventName, string producer, int version)
        {
            var ev = new Event<T>();

            ev.Data = source;
            ev.Name = eventName;
            ev.Producer = producer;
            ev.Version = version;

            return ev;
        }

        public string Id { get; init; }

        public int Version { get; set; }

        public DateTime Time { get; init; }

        public string Name { get; set; }

        public string Producer { get; set; }

        /// <summary>
        /// Message payload
        /// </summary>
        public T Data { get; set; }

    }

    /// <summary>
    /// Any payload event
    /// </summary>
    public class Event : Event<object> { }
}
