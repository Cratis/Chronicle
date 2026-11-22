// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Concepts.Projections.Json;

/// <summary>
/// Represents a <see cref="JsonConverter{T}"/> for read-only dictionaries whose keys are written as the names of JSON properties.
/// </summary>
/// <remarks>
/// Reads and writes the same JSON as the dictionary converter of the definitions did before they became immutable.
/// </remarks>
/// <typeparam name="TKey">Type of key.</typeparam>
/// <typeparam name="TValue">Type of value.</typeparam>
public abstract class ReadOnlyDictionaryJsonConverter<TKey, TValue> : JsonConverter<IReadOnlyDictionary<TKey, TValue>>
    where TKey : notnull
{
    /// <inheritdoc/>
    public override IReadOnlyDictionary<TKey, TValue>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var document = JsonDocument.ParseValue(ref reader);
        var stringDictionary = document.Deserialize<Dictionary<string, TValue>>(options)!;
        return ImmutableDefinitions.Snapshot(stringDictionary.Select(kvp => new KeyValuePair<TKey, TValue>(GetKeyFromString(kvp.Key), kvp.Value!)));
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, IReadOnlyDictionary<TKey, TValue> value, JsonSerializerOptions options)
    {
        if (value is null) return;
        writer.WriteStartObject();
        foreach (var (key, item) in value)
        {
            writer.WritePropertyName(GetKeyString(key));
            writer.WriteRawValue(JsonSerializer.Serialize(item, options), true);
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Get the name of the JSON property for a key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The name of the property.</returns>
    protected virtual string GetKeyString(TKey key) => key.ToString()!;

    /// <summary>
    /// Get the key for the name of a JSON property.
    /// </summary>
    /// <param name="key">The name of the property.</param>
    /// <returns>The key.</returns>
    protected abstract TKey GetKeyFromString(string key);
}
