// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Migrations;

/// <summary>
/// Provides the generation-specific plaintext representation used before target-generation protection.
/// </summary>
internal static class GenerationContentConversion
{
    /// <summary>
    /// Renders schema-converted content as the plaintext that append protects.
    /// </summary>
    /// <param name="content">The schema-converted generation content.</param>
    /// <param name="schema">The generation schema.</param>
    /// <param name="converter">The append converter.</param>
    /// <returns>The generation's plaintext representation.</returns>
    internal static JsonObject ToPlaintext(ExpandoObject content, JsonSchema schema, IExpandoObjectConverter converter) =>
        converter.ToJsonObject(content, schema);

    /// <summary>
    /// Converts one restored protected boundary through the target schema, without provisioning keys.
    /// </summary>
    /// <param name="value">The restored plaintext value.</param>
    /// <param name="boundarySchema">The protected boundary's target schema.</param>
    /// <param name="generationSchema">The generation root used to resolve schema references.</param>
    /// <param name="converter">The append converter.</param>
    /// <returns>The target generation's plaintext value.</returns>
    internal static JsonNode? ConvertProtectedValue(JsonNode value, JsonSchema boundarySchema, JsonSchema generationSchema, IExpandoObjectConverter converter)
    {
        // Use a single-property document so scalar, collection and object boundaries take the same
        // ToExpandoObject/ToJsonObject path as append. References still resolve against the generation root.
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.Properties["value"] = new JsonSchemaProperty("value", JsonNode.Parse(boundarySchema.ToJson())!.AsObject(), generationSchema);
        var document = new JsonObject { ["value"] = value.DeepClone() };
        var converted = converter.ToExpandoObject(document, schema);
        return ToPlaintext(converted, schema, converter)["value"]?.DeepClone();
    }

    /// <summary>
    /// Reproduces the value released after protection encodes the converted node's text.
    /// </summary>
    /// <param name="value">The loss-checked converted plaintext value.</param>
    /// <returns>The released JSON representation, without a CLR-backed scalar.</returns>
    internal static JsonNode? ToReleasedValue(JsonNode value) => value.GetValueKind() == JsonValueKind.String
        ? JsonValue.Create(value.ToString())
        : JsonNode.Parse(value.ToString());
}
