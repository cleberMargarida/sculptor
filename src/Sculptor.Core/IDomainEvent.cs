using System;

namespace Sculptor.Core
{
    /// <summary>  
    /// Represents a domain event.  
    /// </summary>  
    public interface IDomainEvent
    {
        /// <summary>  
        /// Gets the timestamp of when the domain event occurred.  
        /// </summary>  
        public DateTime Timestamp { get; }
    }
}
