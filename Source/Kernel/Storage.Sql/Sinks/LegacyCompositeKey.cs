// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Rebuilds a composite key object from the legacy flattened id the SQL sink stores for it.
/// </summary>
/// <remarks>
/// The sink writes a composite key as its part values, ordered by part name using <see cref="PartOrder"/> and joined
/// with <c language="csharp">_</c>. That format is lossy, so a key is only rebuilt when the stored id can be split
/// and parsed without ambiguity, and when the rebuilt key flattens back to exactly the stored id. Otherwise the
/// caller keeps the key unset. This is the single place where a later key format adds its own decoding.
/// </remarks>
internal sealed class LegacyCompositeKey
{
    const char Separator = '_';
    const string UnsupportedPartReason = "a key part is an object or collection";

    static readonly TypeFormats _typeFormats = new();

    readonly (string Name, Type Type)[] _parts;
    readonly string? _unsupportedReason;

    LegacyCompositeKey((string Name, Type Type)[] parts, string? unsupportedReason)
    {
        _parts = parts;
        _unsupportedReason = unsupportedReason;
    }

    /// <summary>
    /// Gets the order the part names are written in. Must never change, as it defines the stored id.
    /// </summary>
    internal static IComparer<string> PartOrder => Comparer<string>.Default;

    /// <summary>
    /// Creates the rebuilder for a read model, if its key is a composite (object-shaped) key.
    /// </summary>
    /// <param name="schema">The read model schema.</param>
    /// <param name="keyColumn">The <see cref="ProjectedColumn"/> holding the key.</param>
    /// <returns>A <see cref="LegacyCompositeKey"/>, or null if the key is not a composite key.</returns>
    internal static LegacyCompositeKey? For(JsonSchema schema, ProjectedColumn keyColumn)
    {
        if (!schema.Properties.TryGetValue(keyColumn.Name, out var keyProperty) || !IsObjectShaped(keyProperty))
        {
            return null;
        }

        var properties = keyProperty.ActualTypeSchema.ActualProperties;
        if (properties.Count == 0)
        {
            return null;
        }

        if (properties.Values.Any(IsStructured))
        {
            return new LegacyCompositeKey([], UnsupportedPartReason);
        }

        var parts = properties
            .OrderBy(property => property.Key, PartOrder)
            .Select(property =>
            {
                var partSchema = property.Value.ActualTypeSchema;
                var effectiveProperty = new JsonSchemaProperty { Type = partSchema.Type, Format = partSchema.Format };
                var type = effectiveProperty.GetTargetTypeForJsonSchemaProperty(_typeFormats);
                return (property.Key, type is null ? typeof(string) : Nullable.GetUnderlyingType(type) ?? type);
            })
            .ToArray();

        return new LegacyCompositeKey(parts, null);
    }

    /// <summary>
    /// Tries to rebuild the key object from a stored id.
    /// </summary>
    /// <param name="storedId">The flattened id as stored.</param>
    /// <param name="key">The rebuilt key, with each part serialized as a typed column value.</param>
    /// <param name="reason">A fixed phrase, never containing key values, saying why the key was not rebuilt.</param>
    /// <returns>True if the key was rebuilt; false otherwise.</returns>
    internal bool TryRebuild(string storedId, [NotNullWhen(true)] out JsonObject? key, [NotNullWhen(false)] out string? reason)
    {
        key = null;
        if (_unsupportedReason is { } unsupportedReason)
        {
            reason = unsupportedReason;
            return false;
        }

        var segments = storedId.Split(Separator);
        if (segments.Length != _parts.Length)
        {
            reason = "a key part contains '_' or is missing";
            return false;
        }

        if (segments.Any(segment => segment.Length == 0))
        {
            reason = "a key part is empty";
            return false;
        }

        var result = new JsonObject();
        for (var i = 0; i < _parts.Length; i++)
        {
            if (!TryParse(_parts[i].Type, segments[i], out var value))
            {
                reason = "a key part does not parse as its schema type";
                return false;
            }

            if (!IsFinite(value))
            {
                reason = "a key part is not finite";
                return false;
            }

            if (!string.Equals(Convert.ToString(value, CultureInfo.CurrentCulture), segments[i], StringComparison.Ordinal))
            {
                reason = "a key part does not round-trip";
                return false;
            }

            result[_parts[i].Name] = JsonSerializer.SerializeToNode(value, ReadModelDbContext.JsonSerializerOptions);
        }

        key = result;
        reason = null;
        return true;
    }

    static bool IsFinite(object? value) => value switch
    {
        double number => double.IsFinite(number),
        float number => float.IsFinite(number),
        _ => true
    };

    static bool TryParse(Type type, string segment, out object? value)
    {
        value = segment;
        if (type == typeof(string))
        {
            return true;
        }

        try
        {
            value = TypeDescriptor.GetConverter(type).ConvertFromString(null, CultureInfo.CurrentCulture, segment);
            return value is not null;
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or NotSupportedException or OverflowException)
        {
            return false;
        }
    }

    static bool IsObjectShaped(JsonSchemaProperty property)
    {
        if (property.IsArray || HasKnownFormat(property))
        {
            return false;
        }

        if (property.Type.HasFlag(JsonObjectType.Object))
        {
            return true;
        }

        return property.HasReference && property.Reference?.Type.HasFlag(JsonObjectType.Object) == true;
    }

    static bool IsStructured(JsonSchemaProperty property)
    {
        if (HasKnownFormat(property))
        {
            return _typeFormats.IsComplexFormat(property.Format);
        }

        if (property.IsArray || property.Type.HasFlag(JsonObjectType.Object))
        {
            return true;
        }

        return property.HasReference
            && property.Reference is not null
            && (property.Reference.Type.HasFlag(JsonObjectType.Object) || property.Reference.Type.HasFlag(JsonObjectType.Array));
    }

    static bool HasKnownFormat(JsonSchemaProperty property) =>
        !string.IsNullOrEmpty(property.Format) && _typeFormats.IsKnown(property.Format);
}
