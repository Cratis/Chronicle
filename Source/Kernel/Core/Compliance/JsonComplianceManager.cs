// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;
using Cratis.DependencyInjection;
using Cratis.Types;

namespace Cratis.Chronicle.Compliance;

/// <summary>
/// Represents an implementation of <see cref="IJsonComplianceManager"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="JsonComplianceManager"/> class.
/// </remarks>
/// <param name="propertyValueHandlers">Instances of <see cref="IJsonCompliancePropertyValueHandler"/>.</param>
[Singleton]
public class JsonComplianceManager(IInstancesOf<IJsonCompliancePropertyValueHandler> propertyValueHandlers) : IJsonComplianceManager
{
    readonly Dictionary<ComplianceMetadataType, IJsonCompliancePropertyValueHandler> _propertyValueHandlers = propertyValueHandlers.ToDictionary(_ => _.Type, _ => _);

    /// <inheritdoc/>
    public async Task<JsonObject> Apply(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, JsonSchema schema, string identifier, JsonObject json)
    {
        if (!schema.HasComplianceMetadata())
        {
            return json;
        }

        var result = (json.DeepClone() as JsonObject)!;
        await HandleActionFor(schema, identifier, result, "apply", async (h, id, token) => await h.Apply(eventStore, eventStoreNamespace, id, token));
        return result;
    }

    /// <inheritdoc/>
    public async Task<JsonObject> Release(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, JsonSchema schema, string identifier, JsonObject json)
    {
        if (!schema.HasComplianceMetadata())
        {
            return json;
        }

        var result = (json.DeepClone() as JsonObject)!;
        await HandleActionFor(schema, identifier, result!, "release", async (h, id, token) => await h.Release(eventStore, eventStoreNamespace, id, token));
        return result;
    }

    static JsonNode RestoreReleasedContainerShape(JsonNode released, JsonSchema propertySchema)
    {
        // A coarse [PII] on a whole collection is blob-encrypted to a single ciphertext string, even though
        // its schema type stays array. Releasing it decrypts back to the original JSON array text; re-parse
        // that text into the JsonArray the schema expects so the read model round-trips into its collection
        // type rather than a raw string (which fails to deserialize into IReadOnlyList<T>). A scalar PII
        // decrypts to a plain string and is left untouched.
        if (propertySchema.IsArray &&
            released is JsonValue releasedValue &&
            releasedValue.TryGetValue<string>(out var releasedText))
        {
            return JsonNode.Parse(releasedText) ?? released;
        }

        return released;
    }

    async Task HandleActionFor(
        JsonSchema schema,
        string identifier,
        JsonObject json,
        string actionName,
        Func<IJsonCompliancePropertyValueHandler, string, JsonNode, Task<JsonNode>> action,
        string path = "")
    {
        var complianceMetadataForContainer = schema.GetComplianceMetadata();
        foreach (var (property, value) in json.ToArray())
        {
            if (schema.Properties is not null && value is not null)
            {
                var propertyPath = string.IsNullOrEmpty(path) ? property : $"{path}.{property}";
                var propertySchema = schema.GetFlattenedProperties().Single(_ => _.Name == property);
                var handlerApplied = false;
                foreach (var metadata in propertySchema.GetComplianceMetadata().Concat(complianceMetadataForContainer).DistinctBy(_ => _.metadataType))
                {
                    if (_propertyValueHandlers.TryGetValue(metadata.metadataType, out var handler))
                    {
                        try
                        {
                            var handled = await action(handler, identifier, value);
                            json[property] = actionName == "release" ? RestoreReleasedContainerShape(handled, propertySchema) : handled;
                            handlerApplied = true;
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException($"Failed to {actionName} compliance metadata for property '{propertyPath}'.", ex);
                        }
                    }
                }

                if (value is JsonObject jsonObjectValue)
                {
                    await HandleActionFor(propertySchema.ActualTypeSchema, identifier, jsonObjectValue, actionName, action, propertyPath);
                }
                else if (!handlerApplied && value is JsonArray jsonArrayValue)
                {
                    // The property itself was not encrypted as a whole, so descend into the array
                    // and handle compliance metadata that lives on the element type — [PII] scalar
                    // concepts (e.g. IReadOnlyList<Email>) or [PII] members inside element objects.
                    await HandleActionForArray(propertySchema.ActualTypeSchema, identifier, jsonArrayValue, actionName, action, propertyPath);
                }
            }
        }
    }

    async Task HandleActionForArray(
        JsonSchema arraySchema,
        string identifier,
        JsonArray array,
        string actionName,
        Func<IJsonCompliancePropertyValueHandler, string, JsonNode, Task<JsonNode>> action,
        string path)
    {
        var itemSchema = arraySchema.Item?.ActualSchema;
        if (itemSchema is null)
        {
            return;
        }

        var itemComplianceMetadata = itemSchema.GetComplianceMetadata().ToArray();
        for (var i = 0; i < array.Count; i++)
        {
            var element = array[i];
            if (element is null)
            {
                continue;
            }

            var elementPath = $"{path}[{i}]";
            switch (element)
            {
                case JsonObject elementObject:
                    await HandleActionFor(itemSchema, identifier, elementObject, actionName, action, elementPath);
                    break;

                case JsonArray elementArray:
                    await HandleActionForArray(itemSchema, identifier, elementArray, actionName, action, elementPath);
                    break;

                default:
                    foreach (var metadata in itemComplianceMetadata.DistinctBy(_ => _.metadataType))
                    {
                        if (_propertyValueHandlers.TryGetValue(metadata.metadataType, out var handler))
                        {
                            try
                            {
                                array[i] = await action(handler, identifier, element);
                            }
                            catch (Exception ex)
                            {
                                throw new InvalidOperationException($"Failed to {actionName} compliance metadata for property '{elementPath}'.", ex);
                            }
                        }
                    }

                    break;
            }
        }
    }
}
