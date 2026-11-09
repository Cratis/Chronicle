// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Globalization;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes;

/// <summary>
/// Extension methods for inserting new children according to a <see cref="ChildrenOrdering"/>.
/// </summary>
/// <remarks>
/// <see cref="ChildrenPlacement.Prepend"/> inserts a new child first. <see cref="ChildrenPlacement.Sorted"/> orders the whole
/// collection after the insert with a stable sort - children comparing equal keep their relative order - where missing and
/// null values sort first ascending and last descending.
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
    public static void InsertChild<T>(this ICollection<T> collection, T child, ChildrenOrdering? ordering)
    {
        collection.Add(child);
        collection.PlaceAppendedChild(ordering);
    }

    /// <summary>
    /// Move the child most recently appended to a collection to where an ordering places it.
    /// </summary>
    /// <typeparam name="T">Type of item in the collection.</typeparam>
    /// <param name="collection">The collection holding the child as its last item.</param>
    /// <param name="ordering">The optional <see cref="ChildrenOrdering"/>. When null, the child stays where it is.</param>
    /// <remarks>
    /// Appending first and placing afterwards lets a child be populated while it is addressed by its appended position,
    /// and lets <see cref="ChildrenPlacement.Sorted"/> sort on the values the child was populated with.
    /// </remarks>
    public static void PlaceAppendedChild<T>(this ICollection<T> collection, ChildrenOrdering? ordering)
    {
        if (ordering?.IsAppend() != false || collection.Count < 2)
        {
            return;
        }

        if (collection is IList list)
        {
            Place(list, ordering);
            return;
        }

        if (collection is IList<T> typedList)
        {
            if (ordering.Placement == ChildrenPlacement.Prepend)
            {
                var child = typedList[typedList.Count - 1];
                typedList.RemoveAt(typedList.Count - 1);
                typedList.Insert(0, child);
                return;
            }

            var sorted = Sort(typedList.Cast<object>(), ordering).Cast<T>().ToArray();
            typedList.Clear();
            foreach (var item in sorted)
            {
                typedList.Add(item);
            }
        }
    }

    /// <summary>
    /// Compare two values the way children are ordered - missing and null values first, then by natural order.
    /// </summary>
    /// <param name="left">Left value.</param>
    /// <param name="right">Right value.</param>
    /// <returns>Less than zero when left comes before right, zero when equal and greater than zero when after.</returns>
    public static int CompareSortValues(object? left, object? right)
    {
        if (left is null && right is null) return 0;
        if (left is null) return -1;
        if (right is null) return 1;

        if (IsNumeric(left) && IsNumeric(right))
        {
            if (left is float or double || right is float or double)
            {
                return Convert.ToDouble(left, CultureInfo.InvariantCulture).CompareTo(Convert.ToDouble(right, CultureInfo.InvariantCulture));
            }

            return Convert.ToDecimal(left, CultureInfo.InvariantCulture).CompareTo(Convert.ToDecimal(right, CultureInfo.InvariantCulture));
        }

        if (left.GetType() == right.GetType() && left is IComparable comparable)
        {
            return comparable.CompareTo(right);
        }

        return string.CompareOrdinal(AsSortableString(left), AsSortableString(right));
    }

    static void Place(IList collection, ChildrenOrdering ordering)
    {
        if (ordering.Placement == ChildrenPlacement.Prepend)
        {
            var child = collection[collection.Count - 1];
            collection.RemoveAt(collection.Count - 1);
            collection.Insert(0, child);
            return;
        }

        var sorted = Sort(collection.Cast<object>(), ordering).ToArray();
        collection.Clear();
        foreach (var item in sorted)
        {
            collection.Add(item);
        }
    }

    static IEnumerable<object> Sort(IEnumerable<object> items, ChildrenOrdering ordering)
    {
        var comparer = Comparer<object?>.Create(CompareSortValues);
        return ordering.Direction == SortDirection.Descending
            ? items.OrderByDescending(_ => GetSortValue(_, ordering.SortBy), comparer)
            : items.OrderBy(_ => GetSortValue(_, ordering.SortBy), comparer);
    }

    static object? GetSortValue(object? child, PropertyPath sortBy)
    {
        var current = child;
        foreach (var segment in sortBy.Segments)
        {
            if (current is not IDictionary<string, object?> dictionary || !dictionary.TryGetValue(segment.Value, out current))
            {
                return null;
            }
        }

        return current;
    }

    static string? AsSortableString(object value) => value switch
    {
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
        DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
        DateOnly dateOnly => dateOnly.ToString("O", CultureInfo.InvariantCulture),
        TimeOnly timeOnly => timeOnly.ToString("O", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString()
    };

    static bool IsNumeric(object value) => value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;
}
