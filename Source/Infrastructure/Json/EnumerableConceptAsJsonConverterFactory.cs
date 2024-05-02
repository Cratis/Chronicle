// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Concepts;
using Cratis.Reflection;

namespace Cratis.Json;

/// <summary>
/// Represents a <see cref="JsonConverterFactory"/> for providing <see cref="EnumerableConceptAsJsonConverter{T}"/> for enumerable concept types.
/// </summary>
public class EnumerableConceptAsJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsEnumerable() && typeToConvert.IsGenericType && typeToConvert.GetGenericArguments()[0].IsConcept();

    /// <inheritdoc/>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var enumerableConverterType = typeof(EnumerableConceptAsJsonConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]);
        return (Activator.CreateInstance(enumerableConverterType) as JsonConverter)!;
    }
}
