// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Dynamic;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Changes
{
    /// <summary>
    /// Extension methods for working with collections.
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Find an item in a collection by its identity.
        /// </summary>
        /// <typeparam name="TTarget">Target type.</typeparam>
        /// <param name="items">Items to find from.</param>
        /// <param name="identityProperty"><see cref="PropertyPath"/> holding identity on each item.</param>
        /// <param name="key">The key value to check for.</param>
        /// <returns>The item or default if not found.</returns>
        public static TTarget? FindByKey<TTarget>(this IEnumerable<TTarget> items, PropertyPath identityProperty, object key) => items.FirstOrDefault(_ => identityProperty.GetValue(_!)!.Equals(key));

        /// <summary>
        /// Ensures that a collection exists for a specific <see cref="PropertyPath"/>.
        /// </summary>
        /// <typeparam name="TTarget">Target type.</typeparam>
        /// <typeparam name="TChild">Type of child for the collection.</typeparam>
        /// <param name="target">Target object.</param>
        /// <param name="childrenProperty"><see cref="PropertyPath"/> to ensure collection for.</param>
        /// <returns>The ensured <see cref="ICollection{ExpandoObject}"/>.</returns>
        /// <exception cref="ChildrenPropertyIsNotEnumerableForType">Thrown if there is an existing property and it is not enumerable.</exception>
        public static ICollection<TChild> EnsureCollection<TTarget, TChild>(this TTarget target, PropertyPath childrenProperty)
        {
            if (target is ExpandoObject targetAsExpandoObject)
            {
                return targetAsExpandoObject.EnsureCollection<TChild>(childrenProperty);
            }

            var propertyInfo = childrenProperty.GetPropertyInfoFor<TTarget>();
            if (!propertyInfo.PropertyType.IsAssignableFrom(typeof(IEnumerable<TChild>)))
            {
                throw new ChildrenPropertyIsNotEnumerableForType(typeof(TTarget), childrenProperty);
            }

            if (childrenProperty.GetValue(target!) is not ICollection<TChild> items)
            {
                items = new List<TChild>();
                childrenProperty.SetValue(target!, items);
            }

            return items;
        }

        /// <summary>
        /// Check if there is an item with a specific key in a collection of <see cref="ExpandoObject"/> items.
        /// </summary>
        /// <typeparam name="TChild">Type of child.</typeparam>
        /// <param name="items">Items to check.</param>
        /// <param name="identityProperty"><see cref="PropertyPath"/> holding identity on each item.</param>
        /// <param name="key">The key value to check for.</param>
        /// <returns>True if there is an item, false if not.</returns>
        public static bool Contains<TChild>(this IEnumerable<TChild> items, PropertyPath identityProperty, object key)
        {
            if (items is IEnumerable<ExpandoObject> expandoObjectItems) return ExpandoObjectExtensions.Contains(expandoObjectItems, identityProperty, key);
            return items.Any(_ => identityProperty.GetValue(identityProperty)!.Equals(key));
        }
    }
}
