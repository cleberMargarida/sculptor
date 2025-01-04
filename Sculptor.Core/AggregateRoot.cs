using System;

namespace Sculptor.Core
{
    /// <summary>
    /// Represents an abstract base class for aggregate roots with a typed identifier.
    /// </summary>
    /// <typeparam name="TId">The type of the identifier.</typeparam>
    public abstract class AggregateRoot<TId> : Entity<TId>
        where TId : IComparable<TId>
    {
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
