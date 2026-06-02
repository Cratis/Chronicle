// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Represents a <see cref="JsonConverterFactory"/> for dictionaries with concept keys.
/// </summary>
public class ConceptDictionaryJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
        {
            return false;
        }

        var genericDefinition = typeToConvert.GetGenericTypeDefinition();
        var isReadOnly = genericDefinition == typeof(IReadOnlyDictionary<,>);
        if (!isReadOnly && genericDefinition != typeof(IDictionary<,>) && genericDefinition != typeof(Dictionary<,>))
        {
            return false;
        }

        return typeToConvert.GetGenericArguments()[0].IsConcept();
    }

    /// <inheritdoc/>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var keyType = typeToConvert.GetGenericArguments()[0];
        var valueType = typeToConvert.GetGenericArguments()[1];
        var isReadOnly = typeToConvert.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>);
        var converterType = isReadOnly
            ? typeof(ConceptReadOnlyDictionaryJsonConverter<,>).MakeGenericType(keyType, valueType)
            : typeof(ConceptDictionaryJsonConverter<,>).MakeGenericType(keyType, valueType);
        return (Activator.CreateInstance(converterType) as JsonConverter)!;
    }
}
