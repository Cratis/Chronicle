// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Concepts;

namespace Cratis.Json;

/// <summary>
/// Represents a <see cref="JsonConverter{T}"/> for <see cref="ConceptAs{T}"/>.
/// </summary>
/// <typeparam name="TConcept">Underlying concept type.</typeparam>
public class EnumerableConceptAsJsonConverter<TConcept> : JsonConverter<IEnumerable<TConcept>>
{
    readonly ConceptAsJsonConverter<TConcept> _conceptAsJsonConverter;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumerableConceptAsJsonConverter{TConcept}"/>.
    /// </summary>
    public EnumerableConceptAsJsonConverter()
    {
        _conceptAsJsonConverter = new();
    }

    /// <inheritdoc/>
    public override IEnumerable<TConcept>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var items = new List<TConcept>();

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    break;
                }
                var converted = _conceptAsJsonConverter.Read(ref reader, typeof(TConcept), options);
                if (converted is not null)
                {
                    items.Add(converted);
                }
            }
        }

        return items;
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, IEnumerable<TConcept> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value)
        {
            _conceptAsJsonConverter.Write(writer, item, options);
        }
        writer.WriteEndArray();
    }
}
