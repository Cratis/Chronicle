// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Json;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Types;
using NJsonSchema;

namespace Aksio.Cratis.Compliance;

/// <summary>
/// Represents an implementation of <see cref="IJsonComplianceManager"/>.
/// </summary>
public class JsonComplianceManager : IJsonComplianceManager
{
    readonly Dictionary<ComplianceMetadataType, IJsonCompliancePropertyValueHandler> _propertyValueHandlers;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonComplianceManager"/> class.
    /// </summary>
    /// <param name="propertyValueHandlers">Instances of <see cref="IJsonCompliancePropertyValueHandler"/>.</param>
    public JsonComplianceManager(IInstancesOf<IJsonCompliancePropertyValueHandler> propertyValueHandlers)
    {
        _propertyValueHandlers = propertyValueHandlers.ToDictionary(_ => _.Type, _ => _);
    }

    /// <inheritdoc/>
    public async Task<JsonObject> Apply(JsonSchema schema, string identifier, JsonObject json)
    {
        var result = json.DeepClone();
        await HandleActionFor(schema, identifier, result, async (h, id, token) => await h.Apply(id, token));
        return result;
    }

    /// <inheritdoc/>
    public async Task<JsonObject> Release(JsonSchema schema, string identifier, JsonObject json)
    {
        var result = json.DeepClone();
        await HandleActionFor(schema, identifier, result, async (h, id, token) => await h.Release(id, token));
        return result;
    }

    async Task HandleActionFor(JsonSchema schema, string identifier, JsonObject json, Func<IJsonCompliancePropertyValueHandler, string, JsonNode, Task<JsonNode>> action)
    {
        var complianceMetadataForContainer = GetMetadata(schema);
        foreach (var (property, value) in json.ToArray())
        {
            if (schema.Properties is not null && value is not null)
            {
                var propertySchema = schema.ActualProperties.Single(_ => _.Key == property).Value;
                foreach (var metadata in GetMetadata(propertySchema).Concat(complianceMetadataForContainer).DistinctBy(_ => _.metadataType))
                {
                    if (_propertyValueHandlers.ContainsKey(metadata.metadataType))
                    {
                        json[property] = await action(_propertyValueHandlers[metadata.metadataType], identifier, value);
                    }
                }

                if (value is JsonObject jsonObjectValue)
                {
                    await HandleActionFor(propertySchema.ActualTypeSchema, identifier, jsonObjectValue, action);
                }
            }
        }
    }

    IEnumerable<ComplianceSchemaMetadata> GetMetadata(JsonSchema schema)
    {
        if ((schema.ExtensionData?.ContainsKey(JsonSchemaGenerator.ComplianceKey) ?? false) &&
            schema.ExtensionData[JsonSchemaGenerator.ComplianceKey] is IEnumerable<ComplianceSchemaMetadata> allMetadata)
        {
            return allMetadata;
        }

        return Array.Empty<ComplianceSchemaMetadata>();
    }
}
