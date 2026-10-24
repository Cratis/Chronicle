// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Turns the parts of a composite key, given as JSON literals by name, into the typed key of a read model.
/// </summary>
/// <remarks>
/// The key schema is the object-shaped <c language="csharp">id</c> property of the read model schema. Every part is typed using the same
/// <see cref="ITypeFormats"/> the projection engine uses. Part names are matched strictly, so the order the parts are given in
/// does not matter but a missing, extra, unknown or wrongly typed part, or a nested value, is refused.
/// </remarks>
public static class CompositeKeyNormalizer
{
    const string KeyProperty = "id";

    static readonly HashSet<Type> _supportedTypes =
    [
        typeof(string),
        typeof(bool),
        typeof(sbyte),
        typeof(byte),
        typeof(short),
        typeof(ushort),
        typeof(int),
        typeof(uint),
        typeof(long),
        typeof(ulong),
        typeof(float),
        typeof(double),
        typeof(decimal),
        typeof(Guid),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(DateOnly),
        typeof(TimeOnly),
        typeof(TimeSpan)
    ];

    /// <summary>
    /// Normalizes the parts of a composite key.
    /// </summary>
    /// <param name="readModelSchema">The schema of the read model the key belongs to.</param>
    /// <param name="parts">The parts of the key, by name, as JSON literals.</param>
    /// <param name="typeFormats">The <see cref="ITypeFormats"/> used to resolve the type of every part.</param>
    /// <returns>The <see cref="CompositeKeyNormalization"/> holding the typed key or the reason it was refused.</returns>
    public static CompositeKeyNormalization Normalize(JsonSchema readModelSchema, IReadOnlyDictionary<string, string> parts, ITypeFormats typeFormats)
    {
        var keySchema = readModelSchema.GetSchemaPropertyForPropertyPath(KeyProperty)?.ActualTypeSchema;
        if (keySchema?.Type.HasFlag(JsonObjectType.Object) != true || keySchema.IsDictionary || keySchema.ActualProperties.Count == 0)
        {
            return CompositeKeyNormalization.Invalid("The read model does not have a composite key.");
        }

        var schemaParts = keySchema.ActualProperties;
        var unknown = parts.Keys.Where(name => !schemaParts.ContainsKey(name)).Order(StringComparer.Ordinal).ToArray();
        if (unknown.Length > 0)
        {
            return CompositeKeyNormalization.Invalid($"The key has unknown parts: {string.Join(", ", unknown)}.");
        }

        var missing = schemaParts.Keys.Where(name => !parts.ContainsKey(name)).Order(StringComparer.Ordinal).ToArray();
        if (missing.Length > 0)
        {
            return CompositeKeyNormalization.Invalid($"The key is missing parts: {string.Join(", ", missing)}.");
        }

        var key = new ExpandoObject();
        var values = (IDictionary<string, object?>)key;
        foreach (var (name, schemaPart) in schemaParts)
        {
            var type = schemaPart.GetTargetTypeForJsonSchemaProperty(typeFormats);
            if (type is null || !_supportedTypes.Contains(type))
            {
                return CompositeKeyNormalization.Invalid($"The key part '{name}' is not of a type that can be used in a key.");
            }

            if (!TryParse(parts[name], type, out var value))
            {
                return CompositeKeyNormalization.Invalid($"The key part '{name}' is not a valid {type.Name} value.");
            }

            values[name] = value;
        }

        return CompositeKeyNormalization.Valid(key);
    }

    static bool TryParse(string literal, Type type, out object? value)
    {
        value = null;
        try
        {
            using var document = JsonDocument.Parse(literal);
            var kind = document.RootElement.ValueKind;
            if (kind == JsonValueKind.Null)
            {
                return true;
            }

            if (kind is JsonValueKind.Object or JsonValueKind.Array)
            {
                return false;
            }

            value = document.RootElement.Deserialize(type);
            return value is not null;
        }
        catch (Exception ex) when (ex is JsonException or FormatException or InvalidOperationException)
        {
            return false;
        }
    }
}
