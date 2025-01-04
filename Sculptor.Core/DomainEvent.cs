using System;

namespace Sculptor.Core
{
    /// <summary>
    /// Represents a domain event with a specific state and timestamp.
    /// </summary>
    /// <typeparam name="T">The type of the aggregate root associated with the event.</typeparam>
    /// <remarks>
    /// Initializes a new instance of the <see cref="DomainEvent{T}"/> class.
    /// </remarks>
    /// <param name="state">The state of the aggregate root.</param>
    public abstract class DomainEvent<T>(T state) where T : AggregateRoot
    {
        /// <summary>
        /// Gets the timestamp of when the event occurred.
        /// </summary>
        public DateTime Timestamp { get; private set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the state of the aggregate root associated with the event.
        /// </summary>
        public T State { get; private set; } = state;
    }
}
