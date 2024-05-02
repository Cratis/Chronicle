// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Json;

/// <summary>
/// Represents a <see cref="JsonConverter"/> for converting dictionaries to and from JSON.
/// </summary>
/// <typeparam name="TKey">Key of the dictionary.</typeparam>
/// <typeparam name="TValue">Value of the dictionary.</typeparam>
public abstract class DictionaryJsonConverter<TKey, TValue> : JsonConverter<IDictionary<TKey, TValue>>
    where TKey : notnull
{
    /// <inheritdoc/>
    public override IDictionary<TKey, TValue>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var document = JsonDocument.ParseValue(ref reader);
        var stringDictionary = document.Deserialize<Dictionary<string, TValue>>(options)!;
        return stringDictionary.ToDictionary(kvp => GetKeyFromString(kvp.Key), kvp => kvp.Value!);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, IDictionary<TKey, TValue> value, JsonSerializerOptions options)
    {
        if (value is null) return;
        writer.WriteStartObject();
        foreach (var (key, children) in value)
        {
            writer.WritePropertyName(GetKeyString(key!));
            writer.WriteRawValue(JsonSerializer.Serialize(children, options), true);
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Get the string representation of the key.
    /// </summary>
    /// <param name="key">Key to get from.</param>
    /// <returns>String representation.</returns>
    protected virtual string GetKeyString(TKey key) => key.ToString()!;

    /// <summary>
    /// Get the key instance from the string representation.
    /// </summary>
    /// <param name="key">String representation of the key.</param>
    /// <returns>New key instance.</returns>
    protected abstract TKey GetKeyFromString(string key);
}
