// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.Changes
{
    /// <summary>
    /// Extension methods for working with <see cref="Changeset"/>.
    /// </summary>
    public static class ChangesetExtensions
    {
        /// <summary>
        /// Check if changeset contains a <see cref="ChildAdded"/> to a collection with a specific key.
        /// </summary>
        /// <param name="changeset"><see cref="Changeset"/> to check.</param>
        /// <param name="childrenProperty">The <see cref="Property"/> representing the collection.</param>
        /// <param name="key">The key of the item.</param>
        /// <returns>True if it has, false it not.</returns>
        public static bool HasChildBeenAddedWithKey(this Changeset changeset, Property childrenProperty, object key)
        {
            return changeset.Changes
                            .Select(_ => _ as ChildAdded)
                            .Any(_ => _ != null && _.ChildrenProperty == childrenProperty && _.Key == key);
        }
    }
}
