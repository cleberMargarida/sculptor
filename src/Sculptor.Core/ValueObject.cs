using System;
using System.Collections.Generic;
using System.Linq;

namespace Sculptor.Core
{
    /// <summary>
    /// Represents a base class for value objects in the domain model.
    /// </summary>
    public abstract class ValueObject : RichModel
    {
        [HashIgnore]
        private int? _cachedHashCode;

        /// <summary>
        /// Gets the parts of the value object that are used for equality comparison.
        /// </summary>
        /// <returns>An enumerable of objects representing the parts of the value object.</returns>
        protected abstract IEnumerable<object> GetEqualityParts();

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (GetUnproxiedType(this) != GetUnproxiedType(obj))
            {
                return false;
            }

            var valueObject = (ValueObject)obj;

            return GetEqualityParts().SequenceEqual(valueObject.GetEqualityParts());
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            if (!_cachedHashCode.HasValue)
            {
                _cachedHashCode = GetEqualityParts()
                    .Aggregate(1, (current, obj) =>
                    {
                        unchecked
                        {
                            return current * 23 + (obj?.GetHashCode() ?? 0);
                        }
                    });
            }

            return _cachedHashCode.Value;
        }

        /// <summary>
        /// Determines whether two value object instances are equal.
        /// </summary>
        /// <param name="a">The first value object to compare.</param>
        /// <param name="b">The second value object to compare.</param>
        /// <returns>true if the value objects are equal; otherwise, false.</returns>
        public static bool operator ==(ValueObject a, ValueObject b)
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
        /// Determines whether two value object instances are not equal.
        /// </summary>
        /// <param name="a">The first value object to compare.</param>
        /// <param name="b">The second value object to compare.</param>
        /// <returns>true if the value objects are not equal; otherwise, false.</returns>
        public static bool operator !=(ValueObject a, ValueObject b)
        {
            return !(a == b);
        }

        /// <summary>
        /// Gets the unproxied type of the specified object.
        /// </summary>
        /// <param name="obj">The object to get the unproxied type for.</param>
        /// <returns>The unproxied type of the object.</returns>
        internal static Type GetUnproxiedType(object obj)
        {
            const string EFCoreProxyPrefix = "Castle.Proxies.";
            const string NHibernateProxyPostfix = "Proxy";

            Type type = obj.GetType();
            string typeString = type.ToString();

            if (typeString.Contains(EFCoreProxyPrefix) || typeString.EndsWith(NHibernateProxyPostfix))
            {
                return type.BaseType;
            }

            return type;
        }
    }

    /// <summary>
    /// Specifies that a property or field should be ignored when calculating the hash code for source generator.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class HashIgnore : Attribute
    {
    }
}
