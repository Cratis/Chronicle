// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

namespace Cratis.Chronicle.Schemas;

public partial class JsonSchemaGenerator
{
    static Type? GetDictionaryValueType(Type type)
    {
        // Prefer the mutable contract, then choose deterministically if several contracts exist.
        var interfaces = type.GetInterfaces().Prepend(type)
            .Where(_ => _.IsGenericType)
            .OrderBy(_ => _.FullName, StringComparer.Ordinal);
        var dictionary = interfaces.FirstOrDefault(_ => _.GetGenericTypeDefinition() == typeof(IDictionary<,>)) ??
            interfaces.FirstOrDefault(_ => _.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>));
        return dictionary?.GetGenericArguments()[1];
    }

    static bool DictionaryValueIsAnnotatedNullable(Type valueType, ICustomAttributeProvider? attributeProvider)
    {
        var context = new NullabilityInfoContext();
        var nullability = attributeProvider switch
        {
            PropertyInfo property => context.Create(property),
            ParameterInfo parameter => context.Create(parameter),
            _ => null
        };

        // Only use member annotations when its second generic argument is the dictionary value.
        // Derived types with differently arranged arguments cannot supply this annotation reliably.
        return nullability?.GenericTypeArguments is [_, var value] &&
            value.Type == valueType && value.ReadState == NullabilityState.Nullable;
    }

    static bool DictionaryValueIsNullable(Type valueType, ICustomAttributeProvider? attributeProvider) =>
        Nullable.GetUnderlyingType(valueType) is not null || DictionaryValueIsAnnotatedNullable(valueType, attributeProvider);

    static void MakeSchemaNullable(JsonObject schema, bool preserveUnformattedNullability)
    {
        if (schema.TryGetPropertyValue("format", out var format))
        {
            var formatValue = format!.GetValue<string>();
            if (!formatValue.EndsWith('?'))
            {
                schema["format"] = formatValue + "?";
            }
        }
        else if (preserveUnformattedNullability && schema["type"] is JsonValue type)
        {
            schema["type"] = new JsonArray(type.DeepClone(), "null");
        }
    }

    void RestoreConceptDictionaryValueSchema(Type type, ICustomAttributeProvider? attributeProvider, JsonNode schema)
    {
        if (schema is not JsonObject dictionarySchema || GetDictionaryValueType(type) is not { } valueType) return;
        var conceptType = Nullable.GetUnderlyingType(valueType) ?? valueType;
        if (!conceptType.IsConcept()) return;

        // The exporter cannot inspect the value's concept converter and omits additionalProperties.
        // Export the value independently through the scalar repair so its representation and protection
        // metadata survive, while leaving dictionary nullability and property metadata unchanged.
        var valueSchema = _serializerOptions.GetJsonSchemaAsNode(conceptType, _readModelExporterOptions);
        if (valueSchema is JsonObject valueObject && DictionaryValueIsNullable(valueType, attributeProvider))
        {
            MakeSchemaNullable(valueObject, preserveUnformattedNullability: true);
        }
        dictionarySchema["additionalProperties"] = valueSchema;
    }
}
