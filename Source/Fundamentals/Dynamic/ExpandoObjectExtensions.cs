// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using Cratis.Concepts;
using Cratis.Properties;

namespace Cratis.Dynamic
{
    /// <summary>
    /// Extension methods for working with <see cref="ExpandoObject"/>.
    /// </summary>
    public static class ExpandoObjectExtensions
    {
        /// <summary>
        /// Deep clone an <see cref="ExpandoObject"/>.
        /// </summary>
        /// <param name="original">The original <see cref="ExpandoObject"/>.</param>
        /// <returns>A cloned <see cref="ExpandoObject"/>.</returns>
        /// <remarks>
        /// If any of the values represents complex objects, it will not clone these
        /// and create new fresh instances - it will just copy these across.
        /// </remarks>
        public static ExpandoObject Clone(this ExpandoObject original)
        {
            var clone = new ExpandoObject();

            var originalAsDictionary = original as IDictionary<string, object>;
            var cloneAsDictionary = clone as IDictionary<string, object>;

            foreach (var (key, value) in originalAsDictionary)
            {
                cloneAsDictionary.Add(key, value is ExpandoObject child ? child.Clone() : value);
            }

            return clone;
        }

        /// <summary>
        /// Converts an object to a dynamic <see cref="ExpandoObject"/>
        /// </summary>
        /// <param name="original"></param>
        /// <returns>A new <see cref="ExpandoObject"/> representing the given object.</returns>
        public static ExpandoObject AsExpandoObject(this object original)
        {
            var expando = new ExpandoObject();
            var expandoAsDictionary = expando as IDictionary<string, object>;

            foreach (var property in original.GetType().GetProperties())
            {
                var value = property.GetValue(original, null);
                if (value != null)
                {
                    var valueType = value.GetType();
                    if (!valueType.IsPrimitive &&
                        valueType != typeof(string) &&
                        valueType != typeof(Guid) &&
                        !valueType.IsConcept())
                    {
                        value = value.AsExpandoObject();
                    }
                }
                expandoAsDictionary[property.Name] = value!;
            }

            return expando;
        }

        /// <summary>
        /// Creates a clone of left and applies all content from right on top, overwriting any existing properties in left.
        /// </summary>
        /// <param name="left">The left <see cref="ExpandoObject"/>.</param>
        /// <param name="right">The right <see cref="ExpandoObject"/>.</param>
        /// <returns>A new <see cref="ExpandoObject"/>.</returns>
        public static ExpandoObject OverwriteWith(this ExpandoObject left, ExpandoObject right)
        {
            var result = left.Clone();
            var resultAsDictionary = result as IDictionary<string, object>;
            var rightAsDictionary = right as IDictionary<string, object>;

            foreach (var (key, value) in rightAsDictionary)
            {
                var rightValue = value;
                if (resultAsDictionary.ContainsKey(key) && resultAsDictionary[key] is ExpandoObject leftValueExpandoObject &&
                    value is ExpandoObject valueAsExpandoObject)
                {
                    rightValue = leftValueExpandoObject.OverwriteWith(valueAsExpandoObject);
                }
                else if (rightValue is ExpandoObject rightValueAsExpandoObject)
                {
                    rightValue = rightValueAsExpandoObject.Clone();
                }

                resultAsDictionary[key] = rightValue;
            }

            return result;
        }

        /// <summary>
        /// Ensure a specific path for a <see cref="Property"/> exists on an <see cref="ExpandoObject"/>..
        /// </summary>
        /// <param name="target">Target <see cref="ExpandoObject"/>.</param>
        /// <param name="property"><see cref="Property"/> to get or create for.</param>
        /// <returns><see cref="ExpandoObject"/> at property.</returns>
        public static ExpandoObject EnsurePath(this ExpandoObject target, Property property)
        {
            var currentTarget = target as IDictionary<string, object>;
            for (var propertyIndex = 0; propertyIndex < property.Segments.Length - 1; propertyIndex++)
            {
                var segment = property.Segments[propertyIndex];
                if (!currentTarget.ContainsKey(segment))
                {
                    var nested = new ExpandoObject();
                    currentTarget[segment] = nested;
                    currentTarget = nested!;
                }
                else
                {
                    currentTarget = ((ExpandoObject)currentTarget[segment])!;
                }
            }

            return (currentTarget as ExpandoObject)!;
        }

        /// <summary>
        /// Ensures that a collection exists for a specific <see cref="Property"/>.
        /// </summary>
        /// <param name="target">Target <see cref="ExpandoObject"/>.</param>
        /// <param name="childrenProperty"><see cref="Property"/> to ensure collection for.</param>
        /// <returns>The ensured <see cref="ICollection{ExpandoObject}"/>.</returns>
        /// <exception cref="ChildrenPropertyIsNotEnumerable">Thrown if there is an existing property and it is not enumerable.</exception>
        public static ICollection<ExpandoObject> EnsureCollection(this ExpandoObject target, Property childrenProperty)
        {
            var inner = target.EnsurePath(childrenProperty) as IDictionary<string, object>;
            if (!inner.ContainsKey(childrenProperty.LastSegment))
            {
                inner[childrenProperty.LastSegment] = new List<ExpandoObject>();
            }

            if (!(inner[childrenProperty.LastSegment] is IEnumerable))
            {
                throw new ChildrenPropertyIsNotEnumerable(childrenProperty);
            }

            var items = (inner[childrenProperty.LastSegment] as IEnumerable)!.Cast<ExpandoObject>();
            if (items is not IList<ExpandoObject>)
            {
                items = new List<ExpandoObject>(items!);
            }
            inner[childrenProperty.LastSegment] = items;
            return (items as ICollection<ExpandoObject>)!;
        }

        /// <summary>
        /// Check if there is an item with a specific key in a collection of <see cref="ExpandoObject"/> items.
        /// </summary>
        /// <param name="items">Items to check.</param>
        /// <param name="identityProperty"><see cref="Property"/> holding identity on each item.</param>
        /// <param name="key">The key value to check for.</param>
        /// <returns>True if there is an item, false if not</returns>
        public static bool Contains(this IEnumerable<ExpandoObject> items, Property identityProperty, object key) =>
            items!.Any((IDictionary<string, object> _) => _.ContainsKey(identityProperty.Path) && _[identityProperty.Path].Equals(key));

        /// <summary>
        /// Find an item in a collection by its identity.
        /// </summary>
        /// <param name="items">Items to find from.</param>
        /// <param name="identityProperty"><see cref="Property"/> holding identity on each item.</param>
        /// <param name="key">The key value to check for.</param>
        /// <returns>The item or default if not found.</returns>
        public static ExpandoObject? FindByKey(this IEnumerable<ExpandoObject> items, Property identityProperty, object key) =>
            items!.FirstOrDefault((IDictionary<string, object> _) => _.ContainsKey(identityProperty.Path) && _[identityProperty.Path].Equals(key)) as ExpandoObject;
    }
}
