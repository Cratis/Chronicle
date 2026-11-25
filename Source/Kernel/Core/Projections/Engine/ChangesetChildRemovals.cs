// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Inspects a changeset for child removals from every parent without allocating.
/// </summary>
internal static class ChangesetChildRemovals
{
    /// <summary>
    /// Gets whether the changeset holds at least one change and every change is a <see cref="ChildRemovedFromAll"/>.
    /// </summary>
    /// <param name="changeset">The <see cref="IChangeset{TSource, TTarget}"/> to inspect.</param>
    /// <returns>True when the changeset consists only of child removals from every parent; false otherwise, including when it is empty.</returns>
    public static bool OnlyRemovesChildrenFromAll(IChangeset<AppendedEvent, ExpandoObject> changeset) =>
        changeset.Changes is IReadOnlyList<Change> list
            ? list.Count > 0 && IndexOfOtherChange(list) < 0
            : OnlyRemovesChildrenFromAll(changeset.Changes);

    /// <summary>
    /// Gets whether the changeset holds at least one <see cref="ChildRemovedFromAll"/>.
    /// </summary>
    /// <param name="changeset">The <see cref="IChangeset{TSource, TTarget}"/> to inspect.</param>
    /// <returns>True when a child removal from every parent is present.</returns>
    public static bool HasChildRemovalsFromAll(IChangeset<AppendedEvent, ExpandoObject> changeset) =>
        changeset.Changes is IReadOnlyList<Change> list
            ? IndexOfChildRemoval(list) >= 0
            : HasChildRemovalsFromAll(changeset.Changes);

    static int IndexOfOtherChange(IReadOnlyList<Change> list)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i] is not ChildRemovedFromAll)
            {
                return i;
            }
        }

        return -1;
    }

    static int IndexOfChildRemoval(IReadOnlyList<Change> list)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i] is ChildRemovedFromAll)
            {
                return i;
            }
        }

        return -1;
    }

    static bool OnlyRemovesChildrenFromAll(IEnumerable<Change> changes)
    {
        var any = false;
        foreach (var change in changes)
        {
            if (change is not ChildRemovedFromAll)
            {
                return false;
            }

            any = true;
        }

        return any;
    }

    static bool HasChildRemovalsFromAll(IEnumerable<Change> changes)
    {
        foreach (var change in changes)
        {
            if (change is ChildRemovedFromAll)
            {
                return true;
            }
        }

        return false;
    }
}
