// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Concepts;
using Cratis.Types;

namespace Cratis.Json;

/// <summary>
/// Represents a <see cref="JsonConverter{T}"/> for enumerable of concepts or primitive types represented as models with _id field.
/// </summary>
/// <typeparam name="T">Underlying type.</typeparam>
/// <typeparam name="TElement">Element type.</typeparam>
public class EnumerableModelWithIdToConceptOrPrimitiveEnumerableConverter<T, TElement> : JsonConverter<T>
    where T : IEnumerable
{
    readonly ConceptAsJsonConverter<TElement> _conceptAsJsonConverter;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumerableModelWithIdToConceptOrPrimitiveEnumerableConverter{T, TElement}"/> class.
    /// </summary>
    public EnumerableModelWithIdToConceptOrPrimitiveEnumerableConverter()
    {
        _conceptAsJsonConverter = new ConceptAsJsonConverter<TElement>();
    }

    /// <inheritdoc/>
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var values = new List<TElement>();

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    break;
                }

                if (reader.TokenType == JsonTokenType.String)
                {
                    var value = ReadValue(ref reader, options);
                    if (value is not null)
                    {
                        values.Add(value);
                    }
                }
                else if (reader.TokenType == JsonTokenType.StartObject)
                {
                    reader.Read();
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var propertyName = reader.GetString();
                        if (propertyName == "_id")
                        {
                            reader.Read();
                            var value = ReadValue(ref reader, options);
                            if (value is not null)
                            {
                                values.Add(value);
                            }
                        }
                    }
                }
            }
        }

        return (T)values.ToArray().AsEnumerable();
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
    }

    TElement ReadValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var elementType = typeof(TElement);
        if (elementType.IsConcept())
        {
            var conceptInstance = _conceptAsJsonConverter.Read(ref reader, elementType, options);
            if (conceptInstance is not null)
            {
                return conceptInstance;
            }
        }
        else
        {
            return (TElement)TypeConversion.Convert(elementType, reader.GetString()!);
        }

        return default!;
    }
}
