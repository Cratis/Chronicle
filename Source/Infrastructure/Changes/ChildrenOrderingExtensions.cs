// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes;

/// <summary>
/// Extension methods for inserting new children according to a <see cref="ChildrenOrdering"/>.
/// </summary>
/// <remarks>
/// <see cref="ChildrenPlacement.Prepend"/> inserts a new child first. <see cref="ChildrenPlacement.Sorted"/> orders the whole
/// collection after the insert by the sort value, then by the child's identifier ascending, using the culture-free total order
/// of <see cref="ChildrenSortValues"/> - the same order MongoDB uses - so every sink stores the same order.
/// </remarks>
public static class ChildrenOrderingExtensions
{
    /// <summary>
    /// Insert a new child into a collection according to an ordering.
    /// </summary>
    /// <typeparam name="T">Type of item in the collection.</typeparam>
    /// <param name="collection">The collection to insert into.</param>
    /// <param name="child">The child to insert.</param>
    /// <param name="ordering">The optional <see cref="ChildrenOrdering"/>. When null, the child is appended.</param>
    /// <param name="identifiedBy">The optional property identifying a child, breaking ties between equal sort values.</param>
    public static void InsertChild<T>(this ICollection<T> collection, T child, ChildrenOrdering? ordering, PropertyPath? identifiedBy = default)
    {
        collection.Add(child);
        collection.PlaceAppendedChild(ordering, identifiedBy);
    }

    /// <summary>
    /// Move the child most recently appended to a collection to where an ordering places it.
    /// </summary>
    /// <typeparam name="T">Type of item in the collection.</typeparam>
    /// <param name="collection">The collection holding the child as its last item.</param>
    /// <param name="ordering">The optional <see cref="ChildrenOrdering"/>. When null, the child stays where it is.</param>
    /// <param name="identifiedBy">The optional property identifying a child, breaking ties between equal sort values.</param>
    /// <remarks>
    /// Appending first and placing afterwards lets a child be populated while it is addressed by its appended position,
    /// and lets <see cref="ChildrenPlacement.Sorted"/> sort on the values the child was populated with.
    /// </remarks>
    public static void PlaceAppendedChild<T>(this ICollection<T> collection, ChildrenOrdering? ordering, PropertyPath? identifiedBy = default)
    {
        if (ordering?.IsAppend() != false || collection.Count < 2)
        {
            return;
        }

        if (ordering.Placement == ChildrenPlacement.Prepend)
        {
            MoveLastFirst(collection);
            return;
        }

        collection.SortChildren(ordering, identifiedBy);
    }

    /// <summary>
    /// Order a collection again by a <see cref="ChildrenPlacement.Sorted"/> ordering.
    /// </summary>
    /// <typeparam name="T">Type of item in the collection.</typeparam>
    /// <param name="collection">The collection to order.</param>
    /// <param name="ordering">The optional <see cref="ChildrenOrdering"/>. Anything but a sorted ordering leaves the collection as is.</param>
    /// <param name="identifiedBy">The optional property identifying a child, breaking ties between equal sort values.</param>
    /// <remarks>
    /// Used when a child added within the same changeset gets the value it is sorted by after it was placed, so the stored
    /// order is the one the final values give - the same order a sink sorting on insert ends up with.
    /// </remarks>
    public static void SortChildren<T>(this ICollection<T> collection, ChildrenOrdering? ordering, PropertyPath? identifiedBy = default) =>
        collection.SortChildren(ordering, identifiedBy, _ => _);

    /// <summary>
    /// Order a collection again by a <see cref="ChildrenPlacement.Sorted"/> ordering, reading the values to sort by from
    /// another representation of a child than the one in the collection.
    /// </summary>
    /// <typeparam name="T">Type of item in the collection.</typeparam>
    /// <param name="collection">The collection to order.</param>
    /// <param name="ordering">The optional <see cref="ChildrenOrdering"/>. Anything but a sorted ordering leaves the collection as is.</param>
    /// <param name="identifiedBy">The optional property identifying a child, breaking ties between equal sort values.</param>
    /// <param name="valuesOf">Gives the object holding the values a child is sorted by.</param>
    public static void SortChildren<T>(this ICollection<T> collection, ChildrenOrdering? ordering, PropertyPath? identifiedBy, Func<object?, object?> valuesOf)
    {
        if (ordering?.IsAppend() != false || ordering.Placement != ChildrenPlacement.Sorted || collection.Count < 2)
        {
            return;
        }

        var sorted = Sort(collection.Cast<object?>(), ordering, identifiedBy, valuesOf).ToArray();
        switch (collection)
        {
            case IList list:
                list.Clear();
                foreach (var item in sorted)
                {
                    list.Add(item);
                }

                break;

            default:
                collection.Clear();
                foreach (var item in sorted)
                {
                    collection.Add((T)item!);
                }

                break;
        }
    }

    /// <summary>
    /// Compare two values the way children are ordered.
    /// </summary>
    /// <param name="left">Left value.</param>
    /// <param name="right">Right value.</param>
    /// <returns>Less than zero when left comes before right, zero when equal and greater than zero when after.</returns>
    public static int CompareSortValues(object? left, object? right) => ChildrenSortValues.Compare(left, right);

    static void MoveLastFirst<T>(ICollection<T> collection)
    {
        switch (collection)
        {
            case IList list:
                var child = list[list.Count - 1];
                list.RemoveAt(list.Count - 1);
                list.Insert(0, child);
                break;

            case IList<T> typedList:
                var typedChild = typedList[typedList.Count - 1];
                typedList.RemoveAt(typedList.Count - 1);
                typedList.Insert(0, typedChild);
                break;
        }
    }

    static IEnumerable<object?> Sort(IEnumerable<object?> items, ChildrenOrdering ordering, PropertyPath? identifiedBy, Func<object?, object?> valuesOf)
    {
        var comparer = Comparer<object?>.Create(ChildrenSortValues.Compare);
        var ordered = ordering.Direction == SortDirection.Descending
            ? items.OrderByDescending(_ => GetValue(valuesOf(_), ordering.SortBy), comparer)
            : items.OrderBy(_ => GetValue(valuesOf(_), ordering.SortBy), comparer);

        return identifiedBy?.IsSet == true
            ? ordered.ThenBy(_ => GetValue(_, identifiedBy), comparer)
            : ordered;
    }

    static object? GetValue(object? child, PropertyPath path)
    {
        var current = child;
        foreach (var segment in path.Segments)
        {
            if (current is not IDictionary<string, object?> dictionary || !dictionary.TryGetValue(segment.Value, out current))
            {
                return null;
            }
        }

        return current;
    }
}
