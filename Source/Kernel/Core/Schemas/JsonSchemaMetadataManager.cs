// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.DependencyInjection;
using Cratis.Types;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Represents an implementation of <see cref="IJsonSchemaMetadataManager"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="JsonSchemaMetadataManager"/> class.
/// </remarks>
/// <param name="propertyValueHandlers">Instances of <see cref="IJsonSchemaMetadataValueHandler"/>, spanning every registered <see cref="SchemaMetadataCategory"/>.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
[Singleton]
public class JsonSchemaMetadataManager(
    IInstancesOf<IJsonSchemaMetadataValueHandler> propertyValueHandlers,
    ILogger<JsonSchemaMetadataManager> logger) : IJsonSchemaMetadataManager
{
    const string ErasureFenceAction = "apply erasure fence";
    readonly Dictionary<(SchemaMetadataCategory Category, SchemaMetadataTypeName Type), IJsonSchemaMetadataValueHandler> _propertyValueHandlers =
        propertyValueHandlers.ToDictionary(_ => (_.Category, _.Type), _ => _);
    readonly IReadOnlyCollection<SchemaMetadataCategory> _categories = propertyValueHandlers.Select(_ => _.Category).Distinct().ToArray();

    /// <inheritdoc/>
    public async Task<JsonObject> Apply(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, JsonSchema schema, string identifier, JsonObject json)
    {
        if (!schema.HasSchemaMetadata())
        {
            return json;
        }

        var result = (json.DeepClone() as JsonObject)!;
        await HandleActionFor(schema, identifier, result, SchemaMetadataActionFailed.ApplyAction, async (h, id, token) => await h.Apply(eventStore, eventStoreNamespace, id, token));
        return result;
    }

    /// <inheritdoc/>
    public async Task<JsonObject> ApplyToReadModel(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, JsonSchema schema, string identifier, JsonObject json)
    {
        if (!schema.HasSchemaMetadata())
        {
            return json;
        }

        var result = (json.DeepClone() as JsonObject)!;
        await HandleActionFor(schema, identifier, result, SchemaMetadataActionFailed.ApplyAction, async (handler, subject, value) =>
        {
            try
            {
                return await handler.Apply(eventStore, eventStoreNamespace, subject, value);
            }
            catch (EncryptionKeyErased) when (handler.Category == SchemaMetadataCategory.Compliance && handler.Type == ComplianceMetadataType.PII.Value)
            {
                // Match PII release after key deletion without reviving the key or retaining plaintext.
                // Event apply still refuses new personal data for an erased subject.
                return JsonValue.Create(string.Empty);
            }
        });
        return result;
    }

    /// <inheritdoc/>
    public async Task<JsonObject> Release(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, JsonSchema schema, string identifier, JsonObject json)
    {
        if (!schema.HasSchemaMetadata())
        {
            return json;
        }

        var result = (json.DeepClone() as JsonObject)!;
        await HandleActionFor(schema, identifier, result!, SchemaMetadataActionFailed.ReleaseAction, async (h, id, token) => await h.Release(eventStore, eventStoreNamespace, id, token));
        return result;
    }

    /// <inheritdoc/>
    public async Task<JsonObject> ApplyErasureFence(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, JsonSchema schema, string identifier, JsonObject json)
    {
        if (!schema.HasSchemaMetadata())
        {
            return json;
        }

        var result = (json.DeepClone() as JsonObject)!;

        // Erasure is subject-wide. Resolve it once in this walk, not once per property or child,
        // and never keep that decision across operations where the lifecycle may have changed.
        var erasedSubjects = new Dictionary<string, bool>(StringComparer.Ordinal);
        async Task<JsonNode> Fence(IJsonSchemaMetadataValueHandler handler, string subject, JsonNode value)
        {
            var isPII = handler.Category == SchemaMetadataCategory.Compliance && handler.Type == ComplianceMetadataType.PII.Value;
            if (isPII && erasedSubjects.TryGetValue(subject, out var erased))
            {
                return erased ? JsonValue.Create(string.Empty) : value;
            }

            var fenced = await handler.ApplyErasureFence(eventStore, eventStoreNamespace, subject, value);
            if (isPII)
            {
                erasedSubjects[subject] = !ReferenceEquals(fenced, value);
            }

            return fenced;
        }

        await HandleActionFor(schema, identifier, result, ErasureFenceAction, Fence, strictRelease: true);
        return result;
    }

    /// <inheritdoc/>
    public async Task<JsonObject> ReleaseStrict(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, JsonSchema schema, string identifier, JsonObject json)
    {
        if (!schema.HasSchemaMetadata())
        {
            return json;
        }

        var result = (json.DeepClone() as JsonObject)!;
        await HandleActionFor(schema, identifier, result, SchemaMetadataActionFailed.ReleaseAction, async (handler, subject, value) => await handler.ReleaseStrict(eventStore, eventStoreNamespace, subject, value), strictRelease: true);
        return result;
    }

    static JsonNode? RestoreErasedValueShape(JsonNode value, JsonSchema schema) =>
        value is JsonValue scalar && scalar.TryGetValue<string>(out var text) && text.Length == 0
            ? RestoreReleasedContainerShape(value, schema)
            : value;

    static JsonNode? RestoreReleasedContainerShape(JsonNode released, JsonSchema propertySchema)
    {
        propertySchema = propertySchema.ActualTypeSchema;
        var isScalar = propertySchema.Type.HasFlag(JsonObjectType.Integer) ||
            propertySchema.Type.HasFlag(JsonObjectType.Number) || propertySchema.Type.HasFlag(JsonObjectType.Boolean);
        if (isScalar && !propertySchema.Type.HasFlag(JsonObjectType.String) &&
            released is JsonValue scalar && scalar.TryGetValue<string>(out var text))
        {
            // The ciphertext format stores scalar text without a JSON type tag. The schema supplies the
            // missing kind; strings must never be parsed just because their content resembles JSON.
            if (string.IsNullOrEmpty(text))
            {
                if (propertySchema.Type.HasFlag(JsonObjectType.Null) || (propertySchema.Format?.EndsWith('?') ?? false))
                {
                    return null;
                }

                // A parsed JSON number supports every schema numeric format, unlike a JsonValue<int>
                // which cannot be consumed as a double, long or decimal by the converter.
                return propertySchema.Type.HasFlag(JsonObjectType.Boolean) ? JsonValue.Create(false) : JsonNode.Parse("0");
            }

            // Enum names and legacy scalar text must reach the converter unchanged when they do not
            // match the declared JSON kind. Never pass decrypted parser diagnostics to the release log.
            return TryParseReleasedScalar(text, propertySchema, out var parsed) ? parsed : released;
        }

        // A coarse schema metadata marker on a whole container is blob-encrypted to a single ciphertext string,
        // even though its schema type stays array (a collection) or object (a value object). Releasing it
        // decrypts back to the original JSON text; re-parse that text into the container the schema expects so
        // the read model round-trips into its collection or value-object type rather than a raw string (which
        // fails to deserialize). Genuine string scalars are left untouched.
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

    static bool TryParseReleasedScalar(string text, JsonSchema schema, out JsonNode? parsed)
    {
        try
        {
            parsed = JsonNode.Parse(text);
            var kind = parsed?.GetValueKind();
            return schema.Type.HasFlag(JsonObjectType.Boolean)
                ? kind is JsonValueKind.True or JsonValueKind.False
                : kind == JsonValueKind.Number;
        }
        catch (JsonException)
        {
            // Released text is not a JSON scalar. Preserve it for legacy conversion without exposing
            // the parser's value-bearing exception message or substituting a fabricated default.
            parsed = null;
            return false;
        }
    }

    IEnumerable<(SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)> MetadataAcrossCategories(JsonSchema schema) =>
        _categories.SelectMany(category => schema.GetSchemaMetadata(category).Select(metadata => (category, metadata)));

    async Task HandleActionFor(
        JsonSchema schema,
        string identifier,
        JsonObject json,
        string actionName,
        Func<IJsonSchemaMetadataValueHandler, string, JsonNode, Task<JsonNode>> action,
        string path = "",
        bool strictRelease = false)
    {
        var metadataForContainer = MetadataAcrossCategories(schema).ToArray();
        foreach (var (property, value) in json.ToArray())
        {
            if (schema.Properties is not null && value is not null)
            {
                var propertyPath = string.IsNullOrEmpty(path) ? property : $"{path}.{property}";
                var flattenedProperties = schema.GetFlattenedProperties();

                // FirstOrDefault rather than Single: a schema flattened across inheritance can declare the same
                // property name more than once, and the duplicate is not a reason to fail the whole walk.
                var propertySchema = (JsonSchema?)flattenedProperties.FirstOrDefault(_ => _.Name == property) ??
                    schema.AdditionalPropertiesSchema?.ActualSchema ??
                    throw new SchemaPropertyNotFoundInSchema(actionName, propertyPath, identifier, flattenedProperties.Select(_ => _.Name));

                var handlerApplied = false;
                foreach (var (category, metadata) in MetadataAcrossCategories(propertySchema).Concat(metadataForContainer).DistinctBy(_ => (_.Category, _.Metadata.metadataType)))
                {
                    if (_propertyValueHandlers.TryGetValue((category, metadata.metadataType), out var handler))
                    {
                        try
                        {
                            var handled = await action(handler, identifier, value);
                            if (actionName == ErasureFenceAction && ReferenceEquals(handled, value))
                            {
                                handlerApplied = true;
                                continue;
                            }

                            json[property] = actionName switch
                            {
                                SchemaMetadataActionFailed.ReleaseAction => RestoreReleasedContainerShape(handled, propertySchema),
                                ErasureFenceAction => RestoreErasedValueShape(handled, propertySchema),
                                _ => handled
                            };
                            handlerApplied = true;
                        }
                        catch (Exception ex)
                        {
                            var failure = new SchemaMetadataActionFailed(actionName, propertyPath, identifier, ex);

                            // Applying has to fail loudly — storing a value that was never protected is never
                            // acceptable. Releasing must not: a single unreadable property is no reason to fail an
                            // entire query, so surface it as empty — the shape an erased subject already produces
                            // — and keep the diagnostic, which names the property, the identifier and the likely
                            // cause, in the log.
                            if (actionName != SchemaMetadataActionFailed.ReleaseAction || strictRelease)
                            {
                                throw failure;
                            }

                            logger.FailedToReleaseProperty(propertyPath, identifier, failure);
                            json[property] = RestoreReleasedContainerShape(JsonValue.Create(string.Empty), propertySchema);
                            handlerApplied = true;
                        }
                    }
                }

                if (!handlerApplied && value is JsonObject jsonObjectValue && !propertySchema.DescribesGeospatialValue())
                {
                    // Only descend when the property was not handled as a whole. A handled container has already
                    // been replaced by its ciphertext, so recursing would mutate the detached original — the work
                    // is thrown away on apply, and on release it would decrypt members that were never separately
                    // encrypted.
                    //
                    // A geospatial value is an object on the wire but not a container: the schema emits it as a leaf
                    // carrying only its format, so its GeoJSON members are the converter's and there is no schema
                    // property under them for a marker to sit on. Descending would report every one of them as drift
                    // and fail a document that matches its schema. Only the descent is skipped — a value marked
                    // [PII] or [Encrypted] is still handled as a whole above, like any other container.
                    await HandleActionFor(propertySchema.ActualTypeSchema, identifier, jsonObjectValue, actionName, action, propertyPath, strictRelease);
                }
                else if (!handlerApplied && value is JsonArray jsonArrayValue)
                {
                    // The property itself was not encrypted as a whole, so descend into the array and handle
                    // schema metadata that lives on the element type — a [PII]/[Encrypted] scalar concept (e.g.
                    // IReadOnlyList<Email>) or a member marked that way inside element objects.
                    await HandleActionForArray(propertySchema.ActualTypeSchema, identifier, jsonArrayValue, actionName, action, propertyPath, strictRelease);
                }
            }
        }
    }

    async Task HandleActionForArray(
        JsonSchema arraySchema,
        string identifier,
        JsonArray array,
        string actionName,
        Func<IJsonSchemaMetadataValueHandler, string, JsonNode, Task<JsonNode>> action,
        string path,
        bool strictRelease = false)
    {
        var itemSchema = arraySchema.Item?.ActualSchema;
        if (itemSchema is null)
        {
            return;
        }

        var itemMetadata = MetadataAcrossCategories(itemSchema).ToArray();
        for (var i = 0; i < array.Count; i++)
        {
            var element = array[i];
            if (element is null)
            {
                continue;
            }

            var elementPath = $"{path}[{i}]";
            if (element is JsonObject declaredObject && itemSchema.GetFlattenedProperties().Any() && !itemSchema.DescribesGeospatialValue())
            {
                // Declared object members have historically been protected individually, including
                // projection child writes. Only arrays and objects without declared properties need
                // whole-element protection; preserve the persisted member-level representation.
                await HandleActionFor(itemSchema, identifier, declaredObject, actionName, action, elementPath, strictRelease);
                continue;
            }

            var handlerApplied = false;
            foreach (var (category, metadata) in itemMetadata.DistinctBy(_ => (_.Category, _.Metadata.metadataType)))
            {
                if (_propertyValueHandlers.TryGetValue((category, metadata.metadataType), out var handler))
                {
                    try
                    {
                        var handled = await action(handler, identifier, element);
                        if (actionName == ErasureFenceAction && ReferenceEquals(handled, element))
                        {
                            handlerApplied = true;
                            continue;
                        }

                        array[i] = actionName switch
                        {
                            SchemaMetadataActionFailed.ReleaseAction => RestoreReleasedContainerShape(handled, itemSchema),
                            ErasureFenceAction => RestoreErasedValueShape(handled, itemSchema),
                            _ => handled
                        };
                        handlerApplied = true;
                    }
                    catch (Exception ex)
                    {
                        var failure = new SchemaMetadataActionFailed(actionName, elementPath, identifier, ex);

                        // Same asymmetry as the property walk above — apply fails, release degrades the
                        // single element so the rest of the array, and the query, still come back.
                        if (actionName != SchemaMetadataActionFailed.ReleaseAction || strictRelease)
                        {
                            throw failure;
                        }

                        logger.FailedToReleaseProperty(elementPath, identifier, failure);
                        array[i] = RestoreReleasedContainerShape(JsonValue.Create(string.Empty), itemSchema);
                        handlerApplied = true;
                    }
                }
            }

            // A classified element is protected as a whole, including collection-valued elements. Never
            // descend into the detached original or decrypt members that were not separately encrypted.
            if (!handlerApplied && element is JsonObject elementObject && !itemSchema.DescribesGeospatialValue())
            {
                await HandleActionFor(itemSchema, identifier, elementObject, actionName, action, elementPath, strictRelease);
            }
            else if (!handlerApplied && element is JsonArray elementArray)
            {
                await HandleActionForArray(itemSchema, identifier, elementArray, actionName, action, elementPath, strictRelease);
            }
        }
    }
}
