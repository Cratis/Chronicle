// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Extension methods for working with <see cref="ExpandoObject"/>.
    /// </summary>
    public static class ExpandoObjectExtensions
    {
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
    }
}
