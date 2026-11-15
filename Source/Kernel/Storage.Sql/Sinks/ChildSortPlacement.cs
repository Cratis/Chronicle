// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Places a child just appended to a list read back from a JSON column, by the list's ordering.
/// </summary>
/// <remarks>
/// The children read back from the column hold a Guid or a date as the string it is stored as, while the appended
/// child holds typed values. Compare them as the values the schema declares, the way the engine and MongoDB compare
/// them, rather than as strings ranked before every Guid and date.
/// </remarks>
internal static class ChildSortPlacement
{
    static readonly TypeFormats _typeFormats = new();

    /// <summary>
    /// Place a child just appended to a list.
    /// </summary>
    /// <param name="list">The list the child was appended to.</param>
    /// <param name="ordering">The optional ordering of the list.</param>
    /// <param name="identifierName">The property identifying a child, breaking ties between equal sort values.</param>
    /// <param name="childSchema">The optional schema of a child.</param>
    internal static void PlaceAppendedChild(IList<object?> list, ChildrenOrdering? ordering, PropertyPath identifierName, JsonSchema? childSchema)
    {
        if (!NeedsSorting(list, ordering))
        {
            list.PlaceAppendedChild(ordering, identifierName);
            return;
        }

        var sortValue = SortValueReader(ordering!.SortBy, childSchema);
        var comparer = Comparer<object?>.Create(ChildrenOrderingExtensions.CompareSortValues);
        var ordered = ordering.Direction == SortDirection.Descending
            ? list.OrderByDescending(sortValue, comparer)
            : list.OrderBy(sortValue, comparer);
        var sorted = (identifierName.IsSet ? ordered.ThenBy(SortValueReader(identifierName, childSchema), comparer) : ordered).ToArray();

        list.Clear();
        foreach (var child in sorted)
        {
            list.Add(child);
        }
    }

    static bool NeedsSorting(IList<object?> list, ChildrenOrdering? ordering) =>
        list.Count >= 2 && ordering is { Placement: ChildrenPlacement.Sorted } && !ordering.IsAppend();

    static Func<object?, object?> SortValueReader(PropertyPath path, JsonSchema? childSchema)
    {
        var declaredType = childSchema?.GetTargetTypeForPropertyPath(path, _typeFormats);
        var storedAsText = declaredType is not null && JsonColumnCoercion.IsStoredAsText(declaredType) ? declaredType : null;

        return child =>
        {
            var value = JsonColumnDocuments.UnwrapConceptValue(ValueAt(child, path));
            if (value is string text && storedAsText is not null)
            {
                return JsonColumnCoercion.TryConvertStoredText(storedAsText, text);
            }

            // Without a declared type, compare a typed value as the text it is stored as - the form the other children have.
            return storedAsText is null && value is not null && JsonColumnCoercion.IsStoredAsText(value.GetType())
                ? JsonColumnDocuments.ConvertJsonElement(JsonSerializer.SerializeToElement(value, ReadModelDbContext.JsonSerializerOptions))
                : value;
        };
    }

    static object? ValueAt(object? child, PropertyPath path)
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
