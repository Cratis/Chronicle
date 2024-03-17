// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Concepts;
using Cratis.Reflection;

namespace Cratis.Json;

/// <summary>
/// Represents a <see cref="JsonConverterFactory"/> for providing <see cref="EnumerableModelWithIdToConceptOrPrimitiveEnumerableConverter{T, TElement}"/> for
/// enumerable of concepts or primitive types represented as models with _id field.
/// </summary>
public class EnumerableModelWithIdToConceptOrPrimitiveEnumerableConverterFactory : JsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsEnumerable() && typeToConvert.IsGenericType &&
            (typeToConvert.GetGenericArguments()[0].IsConcept() || typeToConvert.IsAPrimitiveType());

    /// <inheritdoc/>
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(EnumerableModelWithIdToConceptOrPrimitiveEnumerableConverter<,>).MakeGenericType(typeToConvert, typeToConvert.GetGenericArguments()[0]);
        return (Activator.CreateInstance(converterType) as JsonConverter)!;
    }
}
