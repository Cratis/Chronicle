// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
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
    static readonly TypeFormats _typeFormats = new();
    static readonly SchemaMetadataCategory[] _categories = [SchemaMetadataCategory.Compliance, SchemaMetadataCategory.Security];

    readonly Dictionary<(SchemaMetadataCategory Category, SchemaMetadataTypeName Type), IJsonSchemaMetadataValueHandler> _propertyValueHandlers =
        propertyValueHandlers.ToDictionary(_ => (_.Category, _.Type), _ => _);

    /// <inheritdoc/>
    public async Task<JsonObject> Apply(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, JsonSchema schema, string identifier, JsonObject json)
    {
        if (!schema.HasSchemaMetadata())
        {
            return json;
        }
        schema.EnsureProtectionCanBeResolved();

        var result = (json.DeepClone() as JsonObject)!;
        await HandleActionFor(schema, identifier, result, SchemaMetadataActionFailed.ApplyAction, async (h, id, token) => new(await h.Apply(eventStore, eventStoreNamespace, id, token)));
        return result;
    }

    /// <inheritdoc/>
    public async Task<JsonObject> ApplyToReadModel(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, JsonSchema schema, string identifier, JsonObject json)
    {
        if (!schema.HasSchemaMetadata())
        {
            return json;
        }
        schema.EnsureProtectionCanBeResolved();

        var result = (json.DeepClone() as JsonObject)!;
        await HandleActionFor(schema, identifier, result, SchemaMetadataActionFailed.ApplyAction, async (h, id, token) => new(await h.Apply(eventStore, eventStoreNamespace, id, token)), erasedValuesBecomePlaceholders: true);
        return result;
    }

    /// <inheritdoc/>
    public async Task<JsonObject> Release(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, JsonSchema schema, string identifier, JsonObject json) =>
        (await ReleaseWithStatus(eventStore, eventStoreNamespace, schema, identifier, json)).Value;

    /// <inheritdoc/>
    public async Task<ReleasedSchemaMetadata> ReleaseWithStatus(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, JsonSchema schema, string identifier, JsonObject json)
    {
        var unreadablePaths = new HashSet<string>(StringComparer.Ordinal);
        if (!schema.HasSchemaMetadata())
        {
            return new(json, unreadablePaths);
        }
        schema.EnsureProtectionCanBeResolved();

        var result = (json.DeepClone() as JsonObject)!;
        await HandleActionFor(schema, identifier, result, SchemaMetadataActionFailed.ReleaseAction, async (h, id, token) => await h.ReleaseWithStatus(eventStore, eventStoreNamespace, id, token), unreadablePaths: unreadablePaths);
        return new(result, unreadablePaths);
    }

    static JsonNode? RestoreReleasedShape(JsonNode released, JsonSchema propertySchema, bool isUnreadable = false)
    {
        if (isUnreadable)
        {
            if (propertySchema.IsNullableSchema() && !propertySchema.IsArray && !propertySchema.Type.HasFlag(JsonObjectType.Object))
            {
                return null;
            }

            released = JsonValue.Create(string.Empty);
        }

        if (released is JsonValue scalar && scalar.TryGetValue<string>(out var text) && text.Length == 0 &&
            !propertySchema.IsArray && !propertySchema.Type.HasFlag(JsonObjectType.Object))
        {
            // Empty text is legitimate for strings, including nullable strings, but cannot represent a typed
            // integer, flag, identifier or date. Unreadable nullable scalars were handled above; legacy empty
            // markers for other typed scalars use the same defaults as schema materialization.
            var targetType = propertySchema.GetTargetTypeForSchema(_typeFormats);
            if (targetType is not null && targetType != typeof(string))
            {
                return JsonSerializer.SerializeToNode(propertySchema.GetDefaultValueForSchema(_typeFormats));
            }
        }

        // A coarse schema metadata marker on a whole container is blob-encrypted to a single ciphertext string,
        // even though its schema type stays array (a collection) or object (a value object). Releasing it
        // decrypts back to the original JSON text; re-parse that text into the container the schema expects so
        // the read model round-trips into its collection or value-object type rather than a raw string (which
        // fails to deserialize). Plain string values remain untouched.
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

        if (released is JsonValue scalarValue && scalarValue.TryGetValue<string>(out var scalarText))
        {
            var actualSchema = propertySchema.ActualTypeSchema;

            // Formatted CLR values (Guid, dates, byte arrays) stringify as quoted JSON, unlike a
            // JsonValue<string>. Remove that JSON quoting without unquoting ordinary personal text.
            var targetType = propertySchema.GetTargetTypeForSchema(_typeFormats);
            if (actualSchema.Type.HasFlag(JsonObjectType.String) && targetType is not null && targetType != typeof(string) && scalarText.StartsWith('"'))
            {
                return JsonNode.Parse(scalarText);
            }

            var enumIndex = actualSchema.EnumerationNames.IndexOf(scalarText);
            if (actualSchema.Type.HasFlag(JsonObjectType.Integer) && enumIndex >= 0 && enumIndex < actualSchema.Enumeration.Count)
            {
                return JsonSerializer.SerializeToNode(actualSchema.Enumeration.ToArray()[enumIndex]);
            }

            // Encryption stores the textual value, not its JSON token kind. Restore numbers and flags
            // using the declared schema; parsing plain strings would turn names like "42" into numbers.
            // Parsing directly also preserves decimal precision rather than rounding through a double.
            if (actualSchema.Type.HasFlag(JsonObjectType.Integer) ||
                actualSchema.Type.HasFlag(JsonObjectType.Number) ||
                actualSchema.Type.HasFlag(JsonObjectType.Boolean))
            {
                return JsonNode.Parse(scalarText);
            }
        }

        return released;
    }

    static JsonNode? ErasedPlaceholder(JsonSchema propertySchema)
    {
        // A container (object/array) marked as a whole is stored the way a crypto-shredded value already reads
        // back: an empty string where the ciphertext would have been. RestoreReleasedShape recognizes exactly
        // that shape on release and turns it back into the empty object or collection the schema expects.
        if (propertySchema.IsArray || propertySchema.Type.HasFlag(JsonObjectType.Object))
        {
            return JsonValue.Create(string.Empty);
        }

        // A scalar leaf is stored here exactly as it is handed to the sink — unlike a genuinely encrypted value,
        // there is no later release pass guaranteed to see this exact placeholder and restore its type (a sink
        // round-trips whatever CLR value it is given). So the placeholder must already carry the declared type
        // RestoreReleasedShape would otherwise reconstruct: an empty string stored where an int, bool or date
        // belongs fails to parse back into that property. Reuse the same type resolution so an erased scalar
        // materializes identically whether it came from a fresh placeholder or a released shredded ciphertext.
        return RestoreReleasedShape(JsonValue.Create(string.Empty), propertySchema, isUnreadable: true);
    }

    (SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)[] MetadataAcrossCategories(JsonSchema schema)
    {
        var entries = _categories.SelectMany(category => schema.GetSchemaMetadata(category).Select(metadata => (Category: category, Metadata: metadata))).ToArray();
        foreach (var entry in entries.Where(_ => !_propertyValueHandlers.ContainsKey((_.Category, _.Metadata.metadataType))))
        {
            throw new UnresolvedSchemaProtection($"no handler for {entry.Category}/{entry.Metadata.metadataType}");
        }
        return entries;
    }

    async Task HandleActionFor(
        JsonSchema schema,
        string identifier,
        JsonObject json,
        string actionName,
        Func<IJsonSchemaMetadataValueHandler, string, JsonNode, Task<ReleasedSchemaMetadataValue>> action,
        string path = "",
        bool erasedValuesBecomePlaceholders = false,
        HashSet<string>? unreadablePaths = null)
    {
        schema = schema.ResolveComposition();
        var metadataForContainer = MetadataAcrossCategories(schema).ToArray();
        foreach (var (property, value) in json.ToArray())
        {
            if (schema.Properties is not null && value is not null)
            {
                var propertyPath = string.IsNullOrEmpty(path) ? property : $"{path}.{property}";
                var flattenedProperties = schema.GetFlattenedProperties();

                JsonSchema? propertySchema = flattenedProperties.SingleOrDefault(_ => _.Name == property);
                if (propertySchema is null && schema.PreservesUnprotectedUnionMembers() &&
                    !flattenedProperties.Any(_ => _.Name.Equals(property, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }
                if (propertySchema is null) throw new SchemaPropertyNotFoundInSchema(actionName, propertyPath, identifier, flattenedProperties.Select(_ => _.Name));
                if (metadataForContainer.Length == 0 && propertySchema.IsUnprotectedSchemaValue()) continue;
                propertySchema = propertySchema.ResolveComposition(protectsValue: metadataForContainer.Length > 0);

                var handlerApplied = false;
                var protection = MetadataAcrossCategories(propertySchema).Concat(metadataForContainer).DistinctBy(_ => (_.Category, _.Metadata.metadataType)).ToArray();
                if (protection.Length > 1) throw new UnresolvedSchemaProtection($"multiple protection handlers for {propertyPath}");
                foreach (var (category, metadata) in protection)
                {
                    if (_propertyValueHandlers.TryGetValue((category, metadata.metadataType), out var handler))
                    {
                        try
                        {
                            var handled = await action(handler, identifier, value);
                            if (handled.IsUnreadable) unreadablePaths?.Add(propertyPath);
                            json[property] = actionName == SchemaMetadataActionFailed.ReleaseAction ? RestoreReleasedShape(handled.Value, propertySchema, handled.IsUnreadable) : handled.Value;
                            handlerApplied = true;
                        }
                        catch (EncryptionKeyErased) when (erasedValuesBecomePlaceholders)
                        {
                            // The erasure fence refused a key for the subject (#4453). A read model is derived state:
                            // whatever arrives here for an erased subject is either what releasing their shredded
                            // values gave back - an empty string, or an empty or default-filled value object - or
                            // personal data that reached the read model in the clear and must not be kept for a
                            // person who asked to be forgotten. Either way the only thing that may be stored is the
                            // erased placeholder; refusing instead would freeze every later update of the partition,
                            // non-personal members included, while protecting nothing. The fence itself is untouched:
                            // no key is created, and appending events keeps refusing through Apply.
                            json[property] = ErasedPlaceholder(propertySchema);
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
                            if (actionName != SchemaMetadataActionFailed.ReleaseAction)
                            {
                                throw failure;
                            }

                            logger.FailedToReleaseProperty(propertyPath, identifier, failure);
                            unreadablePaths?.Add(propertyPath);
                            json[property] = RestoreReleasedShape(JsonValue.Create(string.Empty), propertySchema, isUnreadable: true);
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
                    await HandleActionFor(propertySchema, identifier, jsonObjectValue, actionName, action, propertyPath, erasedValuesBecomePlaceholders, unreadablePaths);
                }
                else if (!handlerApplied && value is JsonArray jsonArrayValue)
                {
                    // The property itself was not encrypted as a whole, so descend into the array and handle
                    // schema metadata that lives on the element type — a [PII]/[Encrypted] scalar concept (e.g.
                    // IReadOnlyList<Email>) or a member marked that way inside element objects.
                    await HandleActionForArray(propertySchema, identifier, jsonArrayValue, actionName, action, propertyPath, erasedValuesBecomePlaceholders, unreadablePaths);
                }
            }
        }
    }

    async Task HandleActionForArray(
        JsonSchema arraySchema,
        string identifier,
        JsonArray array,
        string actionName,
        Func<IJsonSchemaMetadataValueHandler, string, JsonNode, Task<ReleasedSchemaMetadataValue>> action,
        string path,
        bool erasedValuesBecomePlaceholders,
        HashSet<string>? unreadablePaths)
    {
        arraySchema = arraySchema.ResolveComposition();
        var itemSchema = arraySchema.Item?.ResolveComposition();
        if (itemSchema is null)
        {
            return;
        }

        var itemMetadata = MetadataAcrossCategories(itemSchema).DistinctBy(_ => (_.Category, _.Metadata.metadataType)).ToArray();
        if (itemMetadata.Length > 1) throw new UnresolvedSchemaProtection($"multiple protection handlers for {path}");
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
                // A geospatial element is a single typed value, not a container of members, so it falls through to
                // the value branch below — handled as a whole when the element type is marked, left alone when it
                // is not. Walking into it would report its GeoJSON members as drift, the same as for a property.
                case JsonObject elementObject when itemMetadata.Length == 0 && !itemSchema.DescribesGeospatialValue():
                    await HandleActionFor(itemSchema, identifier, elementObject, actionName, action, elementPath, erasedValuesBecomePlaceholders, unreadablePaths);
                    break;

                case JsonArray elementArray when itemMetadata.Length == 0:
                    await HandleActionForArray(itemSchema, identifier, elementArray, actionName, action, elementPath, erasedValuesBecomePlaceholders, unreadablePaths);
                    break;

                default:
                    foreach (var (category, metadata) in itemMetadata.DistinctBy(_ => (_.Category, _.Metadata.metadataType)))
                    {
                        if (_propertyValueHandlers.TryGetValue((category, metadata.metadataType), out var handler))
                        {
                            try
                            {
                                var handled = await action(handler, identifier, element);
                                if (handled.IsUnreadable) unreadablePaths?.Add(elementPath);
                                var restored = actionName == SchemaMetadataActionFailed.ReleaseAction ? RestoreReleasedShape(handled.Value, itemSchema, handled.IsUnreadable) : handled.Value;
                                if (!ReferenceEquals(element, restored))
                                {
                                    array[i] = restored;
                                }
                            }
                            catch (EncryptionKeyErased) when (erasedValuesBecomePlaceholders)
                            {
                                array[i] = ErasedPlaceholder(itemSchema);
                            }
                            catch (Exception ex)
                            {
                                var failure = new SchemaMetadataActionFailed(actionName, elementPath, identifier, ex);

                                // Same asymmetry as the property walk above — apply fails, release degrades the
                                // single element so the rest of the array, and the query, still come back.
                                if (actionName != SchemaMetadataActionFailed.ReleaseAction)
                                {
                                    throw failure;
                                }

                                logger.FailedToReleaseProperty(elementPath, identifier, failure);
                                unreadablePaths?.Add(elementPath);
                                array[i] = RestoreReleasedShape(JsonValue.Create(string.Empty), itemSchema, isUnreadable: true);
                            }
                        }
                    }

                    break;
            }
        }
    }
}
