// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;
using Cratis.DependencyInjection;
using Cratis.Types;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Compliance;

/// <summary>
/// Represents an implementation of <see cref="IJsonComplianceManager"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="JsonComplianceManager"/> class.
/// </remarks>
/// <param name="propertyValueHandlers">Instances of <see cref="IJsonCompliancePropertyValueHandler"/>.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
[Singleton]
public class JsonComplianceManager(
    IInstancesOf<IJsonCompliancePropertyValueHandler> propertyValueHandlers,
    ILogger<JsonComplianceManager> logger) : IJsonComplianceManager
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
        await HandleActionFor(schema, identifier, result, ComplianceMetadataActionFailed.ApplyAction, async (h, id, token) => await h.Apply(eventStore, eventStoreNamespace, id, token));
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
        await HandleActionFor(schema, identifier, result!, ComplianceMetadataActionFailed.ReleaseAction, async (h, id, token) => await h.Release(eventStore, eventStoreNamespace, id, token));
        return result;
    }

    static JsonNode RestoreReleasedContainerShape(JsonNode released, JsonSchema propertySchema)
    {
        // A coarse compliance marker on a whole container is blob-encrypted to a single ciphertext string, even
        // though its schema type stays array (a collection) or object (a value object). Releasing it decrypts
        // back to the original JSON text; re-parse that text into the container the schema expects so the read
        // model round-trips into its collection or value-object type rather than a raw string (which fails to
        // deserialize). A scalar decrypts to a plain string and is left untouched.
        var isContainer = propertySchema.IsArray || propertySchema.Type.HasFlag(JsonObjectType.Object);
        if (isContainer &&
            released is JsonValue releasedValue &&
            releasedValue.TryGetValue<string>(out var releasedText))
        {
            // When the subject's encryption key has been crypto-shredded (GDPR right-to-erasure), the handler
            // surfaces the erased value as an empty string. An erased container reads as empty, so return an
            // empty container rather than letting JsonNode.Parse(string.Empty) throw and poison the release path.
            if (string.IsNullOrWhiteSpace(releasedText))
            {
                return propertySchema.IsArray ? new JsonArray() : new JsonObject();
            }

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
                var flattenedProperties = schema.GetFlattenedProperties();

                // FirstOrDefault rather than Single: a schema flattened across inheritance can declare the same
                // property name more than once, and the duplicate is not a reason to fail the whole walk.
                var propertySchema = flattenedProperties.FirstOrDefault(_ => _.Name == property) ??
                    throw new CompliancePropertyNotFoundInSchema(actionName, propertyPath, identifier, flattenedProperties.Select(_ => _.Name));

                var handlerApplied = false;
                foreach (var metadata in propertySchema.GetComplianceMetadata().Concat(complianceMetadataForContainer).DistinctBy(_ => _.metadataType))
                {
                    if (_propertyValueHandlers.TryGetValue(metadata.metadataType, out var handler))
                    {
                        try
                        {
                            var handled = await action(handler, identifier, value);
                            json[property] = actionName == ComplianceMetadataActionFailed.ReleaseAction ? RestoreReleasedContainerShape(handled, propertySchema) : handled;
                            handlerApplied = true;
                        }
                        catch (Exception ex)
                        {
                            var failure = new ComplianceMetadataActionFailed(actionName, propertyPath, identifier, ex);

                            // Applying has to fail loudly — storing PII that was never protected is never acceptable.
                            // Releasing must not: a single unreadable property is no reason to fail an entire query,
                            // so surface it as empty — the shape an erased subject already produces — and keep the
                            // diagnostic, which names the property, the subject and the likely cause, in the log.
                            if (actionName != ComplianceMetadataActionFailed.ReleaseAction)
                            {
                                throw failure;
                            }

                            logger.FailedToReleaseProperty(propertyPath, identifier, failure);
                            json[property] = RestoreReleasedContainerShape(JsonValue.Create(string.Empty), propertySchema);
                            handlerApplied = true;
                        }
                    }
                }

                if (!handlerApplied && value is JsonObject jsonObjectValue)
                {
                    // Only descend when the property was not handled as a whole. A handled container has already
                    // been replaced by its ciphertext, so recursing would mutate the detached original — the work
                    // is thrown away on apply, and on release it would decrypt members that were never separately
                    // encrypted.
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
                                var failure = new ComplianceMetadataActionFailed(actionName, elementPath, identifier, ex);

                                // Same asymmetry as the property walk above — apply fails, release degrades the
                                // single element so the rest of the array, and the query, still come back.
                                if (actionName != ComplianceMetadataActionFailed.ReleaseAction)
                                {
                                    throw failure;
                                }

                                logger.FailedToReleaseProperty(elementPath, identifier, failure);
                                array[i] = JsonValue.Create(string.Empty);
                            }
                        }
                    }

                    break;
            }
        }
    }
}
