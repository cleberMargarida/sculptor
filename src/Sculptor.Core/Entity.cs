using System;

namespace Sculptor.Core
{
    /// <summary>
    /// Represents an abstract base class for entities with a typed identifier.
    /// </summary>
    /// <typeparam name="TId">The type of the identifier.</typeparam>
    public abstract class Entity<TId> : RichModel, IComparable, IComparable<Entity<TId>>
        where TId : IComparable<TId>
    {
        /// <summary>
        /// Gets the identifier of the entity.
        /// </summary>
        public virtual TId Id { get; protected set; } = default!;

        /// <summary>
        /// Initializes a new instance of the <see cref="Entity{TId}"/> class.
        /// </summary>
        protected Entity()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Entity{TId}"/> class with the specified identifier.
        /// </summary>
        /// <param name="id">The identifier of the entity.</param>
        protected Entity(TId id)
        {
            Id = id;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current entity.
        /// </summary>
        /// <param name="obj">The object to compare with the current entity.</param>
        /// <returns>true if the specified object is equal to the current entity; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (obj is not Entity<TId> other)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (GetType() != other.GetType())
            {
                return false;
            }

            if (IsTransient() || other.IsTransient())
            {
                return false;
            }

            return Id.Equals(other.Id);
        }

        /// <summary>
        /// Determines whether the entity is transient (i.e., has not been assigned an identifier).
        /// </summary>
        /// <returns>true if the entity is transient; otherwise, false.</returns>
        private bool IsTransient()
        {
            return Id is null || Id.Equals(default(TId));
        }

        /// <summary>
        /// Determines whether two entities are equal.
        /// </summary>
        /// <param name="a">The first entity to compare.</param>
        /// <param name="b">The second entity to compare.</param>
        /// <returns>true if the entities are equal; otherwise, false.</returns>
        public static bool operator ==(Entity<TId> a, Entity<TId> b)
        {
            if (a is null && b is null)
            {
                return true;
            }

            if (a is null || b is null)
            {
                return false;
            }

            return a.Equals(b);
        }

        /// <summary>
        /// Determines whether two entities are not equal.
        /// </summary>
        /// <param name="a">The first entity to compare.</param>
        /// <param name="b">The second entity to compare.</param>
        /// <returns>true if the entities are not equal; otherwise, false.</returns>
        public static bool operator !=(Entity<TId> a, Entity<TId> b)
        {
            return !(a == b);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current entity.</returns>
        public override int GetHashCode()
        {
            return (GetType().ToString() + Id).GetHashCode();
        }

        /// <summary>
        /// Compares the current entity with another entity of the same type.
        /// </summary>
        /// <param name="other">The entity to compare with the current entity.</param>
        /// <returns>A value that indicates the relative order of the entities being compared.</returns>
        public virtual int CompareTo(Entity<TId>? other)
        {
            if (other is null)
            {
                return 1;
            }

            if (ReferenceEquals(this, other))
            {
                return 0;
            }

            return Id.CompareTo(other.Id);
        }

        /// <summary>
        /// Compares the current entity with another object.
        /// </summary>
        /// <param name="other">The object to compare with the current entity.</param>
        /// <returns>A value that indicates the relative order of the entities being compared.</returns>
        public virtual int CompareTo(object other)
        {
            return CompareTo(other as Entity<TId>);
        }
    }

    /// <summary>
    /// Represents an abstract base class for entities with a long identifier.
    /// </summary>
    public abstract class Entity : Entity<long>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Entity"/> class.
        /// </summary>
        protected Entity()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Entity"/> class with the specified identifier.
        /// </summary>
        /// <param name="id">The identifier of the entity.</param>
        protected Entity(long id)
            : base(id)
        {
        }
    }
}
