// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using System.Text.Json.Schema;

namespace Cratis.Chronicle.Schemas;

public partial class JsonSchemaGenerator
{
    static bool IsSupportedDictionary(Type type)
    {
        if (!type.IsGenericType) return false;
        var definition = type.GetGenericTypeDefinition();
        return definition == typeof(Dictionary<,>) ||
            definition == typeof(IDictionary<,>) ||
            definition == typeof(IReadOnlyDictionary<,>);
    }

    void RestoreConceptDictionaryValueSchema(Type type, JsonNode schema)
    {
        if (!IsSupportedDictionary(type) || schema is not JsonObject dictionarySchema) return;
        var valueType = type.GetGenericArguments()[1];
        if (!valueType.IsConcept()) return;

        // The exporter cannot inspect the value's concept converter and omits additionalProperties.
        // Export the value independently through the scalar repair so its representation and protection
        // metadata survive, while leaving dictionary nullability and property metadata unchanged.
        dictionarySchema["additionalProperties"] = _serializerOptions.GetJsonSchemaAsNode(valueType, _readModelExporterOptions);
    }
}
