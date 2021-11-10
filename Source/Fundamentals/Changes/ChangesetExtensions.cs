// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Properties;

namespace Cratis.Changes
{
    /// <summary>
    /// Extension methods for working with <see cref="Changeset{TSource, TTarget}"/>.
    /// </summary>
    public static class ChangesetExtensions
    {
        /// <summary>
        /// Check if changeset contains a <see cref="ChildAdded"/> to a collection with a specific key.
        /// </summary>
        /// <typeparam name="TSource">Source type for the changeset.</typeparam>
        /// <typeparam name="TTarget">Target type for the changeset</typeparam>
        /// <param name="changeset"><see cref="Changeset{TSource, TTarget}"/> to check.</param>
        /// <param name="childrenProperty">The <see cref="PropertyPath"/> representing the collection.</param>
        /// <param name="key">The key of the item.</param>
        /// <returns>True if it has, false it not.</returns>
        public static bool HasChildBeenAddedWithKey<TSource, TTarget>(this Changeset<TSource, TTarget> changeset, PropertyPath childrenProperty, object key)
        {
            return changeset.Changes
                            .Select(_ => _ as ChildAdded)
                            .Any(_ => _ != null && _.ChildrenProperty == childrenProperty && _.Key == key);
        }

        /// <summary>
        /// Get a specific child from
        /// </summary>
        /// <typeparam name="TSource">Source type for the changeset.</typeparam>
        /// <typeparam name="TTarget">Target type for the changeset</typeparam>
        /// <typeparam name="TChild">Type of child.</typeparam>
        /// <param name="changeset"><see cref="Changeset{TSource, TTarget}"/> to get from.</param>
        /// <param name="childrenProperty">The <see cref="PropertyPath"/> representing the collection.</param>
        /// <param name="identifiedByProperty">The <see cref="PropertyPath"/> that identifies the child</param>
        /// <param name="key">The key of the item.</param>
        /// <returns>The added child.</returns>
        public static TChild GetChildByKey<TSource, TTarget, TChild>(this Changeset<TSource, TTarget> changeset, PropertyPath childrenProperty, PropertyPath identifiedByProperty, object key)
        {
            var items = childrenProperty.GetValue(changeset.InitialState!) as IEnumerable<TChild>;
            return items!.FindByKey(identifiedByProperty, key)!;
        }
    }
}
