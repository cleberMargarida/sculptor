using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sculptor.Core
{
    /// <summary>
    /// Represents an abstract base class for aggregate roots with a typed identifier.
    /// </summary>
    /// <typeparam name="TId">The type of the identifier.</typeparam>
    public abstract class AggregateRoot<TId> : Entity<TId>, IEventSourcing
        where TId : IComparable<TId>
    {
        [NonSerialized]
        private readonly List<DomainEvent> _events = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateRoot{TId}"/> class.
        /// </summary>
        protected AggregateRoot()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateRoot{TId}"/> class with the specified identifier.
        /// </summary>
        /// <param name="id">The identifier of the aggregate root.</param>
        protected AggregateRoot(TId id)
            : base(id)
        {
        }

        /// <summary>
        /// Adds a domain event for dispatching.
        /// </summary>
        /// <typeparam name="T">The type of the domain event being added.</typeparam>
        /// <param name="event">The domain event instance to add to the collection.</param>
        protected void AddEvent<T>(T @event) 
            where T : DomainEvent
        {
            _events.Add(@event);
        }

        IReadOnlyCollection<DomainEvent> IEventSourcing.Events => _events;

        object IEventSourcing.Id => Id;
    }

    /// <summary>
    /// Represents an event sourcing mechanism that provides access to a collection of domain events.
    /// </summary>
    public interface IEventSourcing
    {
        /// <summary>
        /// Gets the identifier of the entity.
        /// </summary>
        object Id { get; }

        /// <summary>
        /// Retrieves a read-only collection of domain events associated with the current aggregate.
        /// </summary>
        IReadOnlyCollection<DomainEvent> Events { get; }
    }

    /// <summary>
    /// Represents an abstract base class for aggregate roots with a long identifier.
    /// </summary>
    public abstract class AggregateRoot : AggregateRoot<long>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateRoot"/> class.
        /// </summary>
        protected AggregateRoot()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateRoot"/> class with the specified identifier.
        /// </summary>
        /// <param name="id">The identifier of the aggregate root.</param>
        protected AggregateRoot(long id)
            : base(id)
        {
        }
    }
}
