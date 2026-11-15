// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Concepts.Keys;

/// <summary>
/// Represents a <see cref="JsonConverter{T}"/>  that can convert <see cref="Key"/>.
/// </summary>
public class KeyJsonConverter : JsonConverter<Key>
{
    /// <inheritdoc/>
    public override Key? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var arrayIndexers = new List<ArrayIndexer>();
        var value = string.Empty;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }

            var propertyName = reader.GetString();
            if (propertyName == "value")
            {
                reader.Read();
                value = reader.GetString();
            }

            if (propertyName == "arrayIndexers")
            {
                ReadArrayIndexers(ref reader, arrayIndexers);
            }
        }

        return value is null ? Key.Undefined : new Key(value, new ArrayIndexers(arrayIndexers));
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, Key value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("value");
        writer.WriteStringValue(value.Value?.ToString());
        writer.WriteStartArray("arrayIndexers");
        foreach (var arrayIndexer in value.ArrayIndexers.All)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("value");
            writer.WriteStringValue(arrayIndexer.Identifier.ToString());
            writer.WritePropertyName("arrayProperty");
            writer.WriteStringValue(arrayIndexer.ArrayProperty.ToString());
            writer.WritePropertyName("identifierProperty");
            writer.WriteStringValue(arrayIndexer.IdentifierProperty.ToString());
            writer.WritePropertyName("identifier");
            writer.WriteStringValue(arrayIndexer.Identifier.ToString());
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    /// <inheritdoc/>
    public override void WriteAsPropertyName(Utf8JsonWriter writer, Key value, JsonSerializerOptions options) =>
        writer.WritePropertyName(value.ToString());

    /// <inheritdoc/>
    public override Key ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(reader.GetString()!, ArrayIndexers.NoIndexers);

    static void ReadArrayIndexers(ref Utf8JsonReader reader, List<ArrayIndexer> arrayIndexers)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                arrayIndexers.Add(ReadArrayIndexer(ref reader));
            }
        }
    }

    static ArrayIndexer ReadArrayIndexer(ref Utf8JsonReader reader)
    {
        var arrayProperty = string.Empty;
        var identifierProperty = string.Empty;
        var identifier = string.Empty;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }

            var propertyName = reader.GetString();
            reader.Read();
            var value = reader.GetString();
            switch (propertyName)
            {
                case "arrayProperty":
                    arrayProperty = value;
                    break;

                case "identifierProperty":
                    identifierProperty = value;
                    break;

                case "identifier":
                    identifier = value;
                    break;
            }
        }

        return new ArrayIndexer(arrayProperty!, identifierProperty!, identifier!);
    }
}
