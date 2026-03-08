// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Represents a <see cref="JsonConverter"/> for dictionaries with concept keys.
/// </summary>
/// <typeparam name="TKey">The concept key type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
public class ConceptDictionaryJsonConverter<TKey, TValue> : JsonConverter<IDictionary<TKey, TValue>>
    where TKey : notnull
{
    /// <inheritdoc/>
    public override IDictionary<TKey, TValue>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            return new Dictionary<TKey, TValue>();
        }

        var result = new Dictionary<TKey, TValue>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            var key = (TKey)ConceptFactory.CreateConceptInstance(typeof(TKey), reader.GetString()!);

            reader.Read();
            result[key] = JsonSerializer.Deserialize<TValue>(ref reader, options)!;
        }

        return result;
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, IDictionary<TKey, TValue> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var kvp in value)
        {
            var keyString = kvp.Key.GetConceptValue().ToString()!;
            writer.WritePropertyName(keyString);
            JsonSerializer.Serialize(writer, kvp.Value, options);
        }

        writer.WriteEndObject();
    }
}
