// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Kernel.Keys;

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

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                if (propertyName == "value")
                {
                    reader.Read();
                    value = reader.GetString();
                }

                if (propertyName == "arrayIndexers")
                {
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndArray)
                        {
                            break;
                        }

                        if (reader.TokenType == JsonTokenType.StartObject)
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

                                if (reader.TokenType == JsonTokenType.PropertyName)
                                {
                                    var arrayIndexerPropertyName = reader.GetString();
                                    reader.Read();
                                    var arrayIndexerValue = reader.GetString();
                                    switch (arrayIndexerPropertyName)
                                    {
                                        case "arrayProperty":
                                            arrayProperty = arrayIndexerValue;
                                            break;

                                        case "identifierProperty":
                                            identifierProperty = arrayIndexerValue;
                                            break;

                                        case "identifier":
                                            identifier = arrayIndexerValue;
                                            break;
                                    }
                                }
                            }

                            var arrayIndexer = new ArrayIndexer(arrayProperty!, identifierProperty!, identifier!);
                            arrayIndexers.Add(arrayIndexer);
                        }
                    }
                }
            }
        }

        return new Key(value!, new ArrayIndexers(arrayIndexers));
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, Key value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("value");
        writer.WriteStringValue(value.Value.ToString());
        writer.WriteStartArray("arrayIndexers");
        foreach (var arrayIndexer in value.ArrayIndexers.All)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("value");
            writer.WriteStringValue(value.Value.ToString());
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
}
