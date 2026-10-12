// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using System.Globalization;
using System.Text.Json;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Reflection;

#pragma warning disable SA1202 // Private helpers grouped next to the internal methods they back.
#pragma warning disable SA1600 // Internal helpers of the sink, documented by their names.

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Helpers for reading, searching and rewriting the loosely typed documents held in the JSON columns of a SQL read model table,
/// and for coercing values into typed columns.
/// </summary>
internal static class JsonColumnDocuments
{
    internal static object? CoerceForColumn(ProjectedColumn column, object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value.IsConcept())
        {
            value = value.GetConceptValue();
        }

        var clr = Nullable.GetUnderlyingType(column.ClrType) ?? column.ClrType;
        if (clr.IsInstanceOfType(value))
        {
            return value;
        }

        try
        {
            return ConvertToClrType(clr, value);
        }
        catch
        {
            return value;
        }
    }

    internal static object? DeserializeJsonColumn(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(json);
        return ConvertJsonElement(doc.RootElement);
    }

    internal static object? ConvertJsonElement(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => ConvertJsonObject(element),
        JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => ConvertJsonNumber(element),
        JsonValueKind.True or JsonValueKind.False => element.GetBoolean(),
        _ => null
    };

    internal static string SerializeJsonColumn(object? value, ProjectedColumn column)
    {
        if (value is null)
        {
            return column.IsArray ? "[]" : "{}";
        }

        var unwrapped = UnwrapForJson(value);
        return JsonSerializer.Serialize(unwrapped, ReadModelDbContext.JsonSerializerOptions);
    }

    internal static object? UnwrapForJson(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value.IsConcept())
        {
            return value.GetConceptValue();
        }

        if (value is ExpandoObject expando)
        {
            return UnwrapDictionary(expando);
        }

        // A schema-defined dictionary property (additionalProperties) round-trips as
        // Dictionary<object, object> (not IDictionary<string, object?>), because its keys and values
        // are typed by whatever the schema declares. Falling through to the generic IEnumerable branch
        // below would iterate its KeyValuePair<,> entries as plain elements, serializing the dictionary
        // as a JSON array of { "Key": ..., "Value": ... } objects instead of a JSON object.
        if (value.GetType().IsDictionary() && value is IEnumerable dictionaryEntries)
        {
            return UnwrapKeyValuePairs(dictionaryEntries);
        }

        if (value is string)
        {
            return value;
        }

        return value is IEnumerable enumerable ? UnwrapEnumerable(enumerable) : value;
    }

    internal static object? UnwrapConceptValue(object? value) => value?.IsConcept() == true ? value.GetConceptValue() : value;

    internal static PropertyPath BuildSubPath(PropertyPath path)
    {
        var segments = path.Segments.Skip(1).ToArray();
        if (segments.Length == 0)
        {
            return new PropertyPath(string.Empty);
        }

        return PropertyPath.CreateFrom(segments);
    }

    internal static bool TryFindValueInDeserialized(object? node, IPropertyPathSegment[] segments, int index, object target)
    {
        if (node is null || index >= segments.Length)
        {
            return false;
        }

        return node switch
        {
            IList<object?> list => list.Any(item => TryFindValueInDeserialized(item, segments, index, target)),
            IDictionary<string, object?> dict => TryFindValueInDictionary(dict, segments, index, target),
            _ => index == segments.Length - 1 && CompareLoose(node, target)
        };
    }

    internal static bool CompareLoose(object? value, object? target)
    {
        if (value is null || target is null)
        {
            return value is null && target is null;
        }

        if (value.Equals(target) || MatchesConceptValue(value, target))
        {
            return true;
        }

        return string.Equals(value.ToString(), target.ToString(), StringComparison.Ordinal);
    }

    internal static bool RemoveChildrenFromPath(object? node, int index, ChildRemoval removal)
    {
        if (index == removal.Segments.Length)
        {
            return RemoveMatchingChildren(node, removal);
        }

        if (node is IDictionary<string, object?> nested)
        {
            return nested.TryGetValue(removal.Segments[index].Value, out var value) &&
                RemoveChildrenFromPath(value, index + 1, removal);
        }

        if (node is not IList<object?> parents)
        {
            return false;
        }

        var modified = false;
        foreach (var parent in parents)
        {
            modified |= RemoveChildrenFromPath(parent, index, removal);
        }

        return modified;
    }

    internal static bool IsRemovedChild(object? item, PropertyPath identifier, object? key, string? keyAsString)
    {
        if (!ChildIdentity.TryGetValue(item, identifier, out var value))
        {
            return false;
        }

        return Equals(value, key) || (value is not null && value.ToString() == keyAsString);
    }

    internal static ArrayIndexers RewriteIndexersRelativeTo(string columnName, ArrayIndexers indexers)
    {
        // The change's indexers carry absolute paths from the read model root (e.g.
        // "Features.Slices"). After we have descended into the column's array element we hand
        // EnsurePath a sub-context, and it walks paths relative to that element. Drop the column
        // prefix from every indexer's ArrayProperty / IdentifierProperty so they match the path
        // segments EnsurePath sees while traversing the subPath.
        var rewritten = new List<ArrayIndexer>();
        foreach (var indexer in indexers.All)
        {
            var arraySegments = indexer.ArrayProperty.Segments.ToArray();
            if (!StartsWithColumn(arraySegments, columnName))
            {
                rewritten.Add(indexer);
                continue;
            }

            // An indexer for the column itself is no longer needed inside its element.
            if (arraySegments.Length > 1)
            {
                rewritten.Add(TrimColumnPrefix(indexer, arraySegments, columnName));
            }
        }

        return new ArrayIndexers(rewritten);
    }

    internal static ExpandoObject LocateOrCreateChild(IList<object?> list, ArrayIndexer indexer)
    {
        var identifier = ChildIdentifier.For(indexer.IdentifierProperty, UnwrapConceptValue(indexer.Identifier));

        var index = FindChildIndex(list, identifier, true);
        if (index >= 0)
        {
            return PromoteToExpando(list, index);
        }

        var fresh = new ExpandoObject();
        identifier.Name.SetValue(fresh, identifier.Value!, ArrayIndexers.NoIndexers);
        list.Add(fresh);
        return fresh;
    }

    internal static void ReplaceChildContents(ExpandoObject target, object? replacement)
    {
        var dict = (IDictionary<string, object?>)target;
        dict.Clear();
        if (replacement is IDictionary<string, object?> source)
        {
            CopyInto(dict, source);
        }
    }

    internal static void UpsertChildInList(IList<object?> list, PropertyPath identifierName, object? identifierValue, object? newChild)
    {
        // Upsert by identifier so repeated ChildAdded events (e.g. during replay, or when a
        // projection re-introduces the same child after a redaction) update in place instead of
        // appending duplicates. Merge into the existing entry so that any child collections
        // populated by earlier ChildAdded events (e.g. Slices.Events) survive the replacement of
        // the parent's own properties — a wholesale replace would clobber them.
        var index = FindChildIndex(list, ChildIdentifier.For(identifierName, identifierValue), false);
        if (index < 0)
        {
            list.Add(AsExpando(newChild));
            return;
        }

        if (newChild is IDictionary<string, object?> incoming)
        {
            CopyInto((IDictionary<string, object?>)list[index]!, incoming);
        }
        else
        {
            list[index] = AsExpando(newChild);
        }
    }

    internal static void UpsertNestedChild(IList<object?> parentList, int index, ChildUpsert upsert)
    {
        var parentSegmentName = upsert.Segments[index - 1].Value;
        var indexer = upsert.Indexers.All.FirstOrDefault(i => i.ArrayProperty.LastSegment.Value == parentSegmentName);
        if (indexer is null)
        {
            return;
        }

        var parentIdentifier = ChildIdentifier.For(indexer.IdentifierProperty, UnwrapConceptValue(indexer.Identifier));

        var parentDict = parentList
            .OfType<IDictionary<string, object?>>()
            .FirstOrDefault(parent => ChildIdentity.TryGetValue(parent, parentIdentifier.Name, out var matchValue) && IdentifierMatches(matchValue, parentIdentifier, true));
        if (parentDict is null)
        {
            return;
        }

        var childList = GetOrCreateChildList(parentDict, upsert.Segments[index].Value);
        if (index == upsert.Segments.Length - 1)
        {
            UpsertChildInList(childList, upsert.IdentifierName, upsert.IdentifierValue, upsert.NewChild);
            return;
        }

        UpsertNestedChild(childList, index + 1, upsert);
    }

    static object? ConvertToClrType(Type clr, object value)
    {
        if (clr.IsEnum)
        {
            return Convert.ChangeType(value, Enum.GetUnderlyingType(clr), CultureInfo.InvariantCulture);
        }

        if (clr == typeof(Guid))
        {
            return value is string s ? Guid.Parse(s) : value;
        }

        if (value is string text && ParseTemporalText(clr, text) is { } parsed)
        {
            return parsed;
        }

        return Convert.ChangeType(value, clr, CultureInfo.InvariantCulture);
    }

    static object? ParseTemporalText(Type clr, string text)
    {
        if (clr == typeof(DateTime))
        {
            return DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        if (clr == typeof(DateTimeOffset))
        {
            return DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        if (clr == typeof(DateOnly))
        {
            return DateOnly.Parse(text, CultureInfo.InvariantCulture);
        }

        return clr == typeof(TimeOnly) ? TimeOnly.Parse(text, CultureInfo.InvariantCulture) : null;
    }

    static ExpandoObject ConvertJsonObject(JsonElement element)
    {
        var expando = new ExpandoObject();
        var dict = (IDictionary<string, object?>)expando;
        foreach (var property in element.EnumerateObject())
        {
            dict[property.Name] = ConvertJsonElement(property.Value);
        }

        return expando;
    }

    static object ConvertJsonNumber(JsonElement element)
    {
        if (element.TryGetInt64(out var integer))
        {
            return integer;
        }

        if (element.TryGetDecimal(out var decimalValue))
        {
            return decimalValue;
        }

        return element.GetDouble();
    }

    static Dictionary<string, object?> UnwrapDictionary(IDictionary<string, object?> source)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var kvp in source)
        {
            dict[kvp.Key] = UnwrapForJson(kvp.Value);
        }

        return dict;
    }

    static List<object?> UnwrapEnumerable(IEnumerable enumerable)
    {
        var list = new List<object?>();
        foreach (var item in enumerable)
        {
            list.Add(UnwrapForJson(item));
        }

        return list;
    }

    static Dictionary<string, object?> UnwrapKeyValuePairs(IEnumerable entries)
    {
        var result = new Dictionary<string, object?>();
        foreach (var entry in entries.GetKeyValuePairs())
        {
            result[entry.Key] = UnwrapForJson(entry.Value);
        }

        return result;
    }

    static bool TryFindValueInDictionary(IDictionary<string, object?> dict, IPropertyPathSegment[] segments, int index, object target)
    {
        if (!dict.TryGetValue(segments[index].Value, out var value))
        {
            return false;
        }

        return index == segments.Length - 1
            ? CompareLoose(value, target)
            : TryFindValueInDeserialized(value, segments, index + 1, target);
    }

    static bool MatchesConceptValue(object value, object target) =>
        (target.IsConcept() && value.Equals(target.GetConceptValue())) ||
        (value.IsConcept() && (value.GetConceptValue()?.Equals(target) ?? false));

    static bool RemoveMatchingChildren(object? node, ChildRemoval removal)
    {
        if (node is not IList<object?> children)
        {
            return false;
        }

        var removed = false;
        for (var childIndex = children.Count - 1; childIndex >= 0; childIndex--)
        {
            if (IsMatchingChild(children[childIndex], removal))
            {
                children.RemoveAt(childIndex);
                removed = true;
            }
        }

        return removed;
    }

    static bool IsMatchingChild(object? child, ChildRemoval removal) =>
        ChildIdentity.TryGetValue(child, removal.Identifier, out var value) &&
        CompareLoose(value, removal.Key);

    static bool StartsWithColumn(IPropertyPathSegment[] segments, string columnName) =>
        segments.Length > 0 && string.Equals(segments[0].Value, columnName, StringComparison.Ordinal);

    static ArrayIndexer TrimColumnPrefix(ArrayIndexer indexer, IPropertyPathSegment[] arraySegments, string columnName)
    {
        var trimmedArray = PropertyPath.CreateFrom(arraySegments.Skip(1).ToArray());
        var identifierSegments = indexer.IdentifierProperty.Segments.ToArray();
        var trimmedIdentifier = StartsWithColumn(identifierSegments, columnName)
            ? PropertyPath.CreateFrom(identifierSegments.Skip(1).ToArray())
            : indexer.IdentifierProperty;

        // Normalize identifiers to string form. JSON round-trips turn Guid identifiers into
        // strings and strip ConceptAs<T> wrappers, so the inside-EnsurePath comparison of the
        // stored value (string) against the original Identifier (concept/Guid) never matches
        // and manufactures a duplicate entry instead of finding the existing one. Comparing
        // the two as strings makes the lookup deterministic across types.
        var normalizedIdentifier = UnwrapConceptValue(indexer.Identifier)?.ToString();
        return new ArrayIndexer(trimmedArray, trimmedIdentifier, normalizedIdentifier!);
    }

    static bool IdentifierMatches(object? stored, ChildIdentifier identifier, bool eagerStoredConversion)
    {
        // The identifier is converted to a string once, up front. The stored value is converted before the equality
        // check when eagerStoredConversion is set, and only when equality fails otherwise.
        if (eagerStoredConversion)
        {
            var storedAsString = stored?.ToString();
            return Equals(stored, identifier.Value) || string.Equals(storedAsString, identifier.AsString, StringComparison.Ordinal);
        }

        return Equals(stored, identifier.Value) || stored?.ToString() == identifier.AsString;
    }

    static int FindChildIndex(IList<object?> list, ChildIdentifier identifier, bool eagerStoredConversion)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (!ChildIdentity.TryGetValue(list[i], identifier.Name, out var stored))
            {
                continue;
            }

            if (IdentifierMatches(stored, identifier, eagerStoredConversion))
            {
                return i;
            }
        }

        return -1;
    }

    static ExpandoObject PromoteToExpando(IList<object?> list, int index)
    {
        if (list[index] is ExpandoObject existing)
        {
            return existing;
        }

        // Reify the dictionary as an ExpandoObject so EnsurePath can navigate it.
        var promoted = ToExpando((IDictionary<string, object?>)list[index]!);
        list[index] = promoted;
        return promoted;
    }

    static ExpandoObject ToExpando(IDictionary<string, object?> source)
    {
        var expando = new ExpandoObject();
        CopyInto(expando, source);
        return expando;
    }

    static void CopyInto(IDictionary<string, object?> target, IDictionary<string, object?> source)
    {
        foreach (var kvp in source)
        {
            target[kvp.Key] = kvp.Value is IDictionary<string, object?> nested ? ToExpando(nested) : kvp.Value;
        }
    }

    static object? AsExpando(object? value)
    {
        // EnsurePath walks ExpandoObject roots and filters list items via OfType<ExpandoObject>();
        // any dictionary in the list that isn't an actual ExpandoObject becomes invisible to the
        // path walker, which then creates a *second* element at the same identifier instead of
        // mutating the existing one. Promote Dictionary<string, object?> values (the form
        // UnwrapForJson produces from ExpandoObject) to ExpandoObject before they enter the list.
        return value switch
        {
            ExpandoObject => value,
            IDictionary<string, object?> dict => ToExpando(dict),
            _ => value
        };
    }

    static IList<object?> GetOrCreateChildList(IDictionary<string, object?> parent, string childCollectionName)
    {
        if (parent.TryGetValue(childCollectionName, out var existing) && existing is IList<object?> existingList)
        {
            return existingList;
        }

        var created = new List<object?>();
        parent[childCollectionName] = created;
        return created;
    }

    internal sealed class ChildIdentifier
    {
        ChildIdentifier(PropertyPath name, object? value)
        {
            Name = name;
            Value = value;
            AsString = value?.ToString();
        }

        internal PropertyPath Name { get; }

        internal object? Value { get; }

        internal string? AsString { get; }

        internal static ChildIdentifier For(PropertyPath name, object? value) => new(name, value);
    }

    /// <summary>
    /// Describes the removal of a child from every collection found at a path.
    /// </summary>
    /// <param name="Segments">The segments of the path to the children collection.</param>
    /// <param name="Identifier">The name of the property identifying a child.</param>
    /// <param name="Key">The identifier value of the child to remove.</param>
    internal sealed record ChildRemoval(IPropertyPathSegment[] Segments, PropertyPath Identifier, object? Key);

    /// <summary>
    /// Describes the upsert of a child into a collection nested in other collections.
    /// </summary>
    /// <param name="Segments">The segments of the path to the children collection.</param>
    /// <param name="Indexers">The <see cref="ArrayIndexers"/> locating the parents.</param>
    /// <param name="IdentifierName">The name of the property identifying a child.</param>
    /// <param name="IdentifierValue">The identifier value of the child.</param>
    /// <param name="NewChild">The child to upsert.</param>
    internal sealed record ChildUpsert(IPropertyPathSegment[] Segments, ArrayIndexers Indexers, PropertyPath IdentifierName, object? IdentifierValue, object? NewChild);
}
