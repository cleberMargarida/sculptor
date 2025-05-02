using System;

namespace Sculptor.Core
{
    /// <summary>
    /// Represents a domain event.
    /// </summary>
    public interface IDomainEvent
    {
        public DateTime Timestamp { get; }
    }
}
