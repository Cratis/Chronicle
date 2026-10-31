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
    public async Task<(JsonObject Value, IReadOnlyList<ProtectedValueUse> Uses)> ApplyReportingKeys(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, JsonSchema schema, string identifier, JsonObject json)
    {
        var result = (JsonObject)json.DeepClone();
        var uses = new List<ProtectedValueUse>();
        if (!schema.HasSchemaMetadata())
        {
            return (result, uses);
        }

        await HandleActionFor(schema, identifier, result, SchemaMetadataActionFailed.ApplyAction, (_, _, value) => Task.FromResult(value), boundaryTransform: async (path, handlers, subject, value) =>
        {
            ProtectionLocation(path, handlers, subject);
            var plaintext = value.DeepClone();
            var applied = await handlers[0].ApplyReportingKey(eventStore, eventStoreNamespace, subject, value);
            var key = applied.Key ?? throw new ProtectedBoundaryUnsupported(path);
            uses.Add(new(path, key.Kind, plaintext, key));
            return applied.Value;
        });
        return (result, uses.OrderBy(_ => _.Path, StringComparer.Ordinal).ToArray());
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ProtectedValueLocation>> LocateProtectedValues(JsonSchema schema, string identifier, JsonObject json)
    {
        var locations = new List<ProtectedValueLocation>();
        if (!schema.HasSchemaMetadata())
        {
            return locations;
        }

        await HandleActionFor(schema, identifier, (JsonObject)json.DeepClone(), SchemaMetadataActionFailed.ApplyAction, (_, _, value) => Task.FromResult(value), boundaryTransform: (path, handlers, subject, value) =>
        {
            var location = ProtectionLocation(path, handlers, subject);
            locations.Add(new(path, location.Kind, location.Identifier, value.DeepClone()));
            return Task.FromResult(value);
        });
        return locations.OrderBy(_ => _.Path, StringComparer.Ordinal).ToArray();
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
    public async Task<(JsonObject Value, IReadOnlyList<string> ErasedPaths)> ReleaseReportingErasedProperties(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, JsonSchema schema, string identifier, JsonObject json)
    {
        var result = (JsonObject)json.DeepClone();
        var erasure = new ErasedPathCollector(eventStore, eventStoreNamespace);
        if (schema.HasSchemaMetadata())
        {
            await WalkObject(new MetadataWalk { Identifier = identifier, ActionName = SchemaMetadataActionFailed.ReleaseAction, Action = static (_, _, value) => Task.FromResult(value), Erasure = erasure }, schema, result, string.Empty);
        }

        return (result, erasure.Paths.Order(StringComparer.Ordinal).ToArray());
    }

    /// <inheritdoc/>
    public async Task<JsonObject?> TryRelease(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, JsonSchema schema, string identifier, JsonObject json)
    {
        if (!schema.HasSchemaMetadata())
        {
            return (JsonObject)json.DeepClone();
        }

        try
        {
            return await TryTransformForComparison(schema, json, (current, handler, node) =>
                StrictJsonSchemaRelease.ReleaseValue(current, handler, eventStore, eventStoreNamespace, identifier, node));
        }
        catch (Exception exception)
        {
            // A failed release is an explicit unavailable result, never a partially released document.
            logger.FailedToReleaseProperty(string.Empty, identifier, exception);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<JsonObject?> TryPrepareForComparison(JsonSchema schema, JsonObject json, Func<JsonObject, JsonObject?> convert)
    {
        var generations = await TryPrepareGenerationsForComparison(schema, json, document =>
        {
            var converted = convert(document);
            return Task.FromResult<IReadOnlyDictionary<int, (JsonSchema Schema, JsonObject Content)>?>(
                converted is null ? null : new Dictionary<int, (JsonSchema, JsonObject)> { [1] = (schema, converted) });
        });
        return generations?[1];
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<int, JsonObject>?> TryPrepareGenerationsForComparison(
        JsonSchema schema,
        JsonObject json,
        Func<JsonObject, Task<IReadOnlyDictionary<int, (JsonSchema Schema, JsonObject Content)>?>> convert,
        Func<int, JsonSchema, JsonNode, JsonNode?>? convertProtectedValue = null)
    {
        // Append protects the attempted generation BEFORE schema conversion; migrated generations convert first.
        // Use opaque markers, not encryption: verification must never provision keys, and restored values
        // must be loss-checked against their generation's conversion before comparison.
        // Markers carry a random nonce per call, so a migration cannot name, synthesize or map another value onto one.
        // Content that already looks like a marker cannot be told apart from one, so it is never compared.
        if (VerificationMarkers.AppearIn(json))
        {
            return null;
        }

        var markerPrefix = VerificationMarkers.NewPrefix();
        var values = new Dictionary<string, JsonNode>();
        var masked = !schema.HasSchemaMetadata() ? json : await TryTransformForComparison(schema, json, (_, _, node) =>
        {
            var marker = $"{markerPrefix}{values.Count}";
            values.Add(marker, node.DeepClone());
            return Task.FromResult<JsonNode?>(JsonValue.Create(marker));
        });
        var converted = masked is null ? null : await convert(masked);
        if (converted is null)
        {
            return null;
        }

        var result = new Dictionary<int, JsonObject>();
        foreach (var (generation, content) in converted)
        {
            // Each marker restores at most once per generation, and only at a protected location. A marker that
            // was duplicated, moved to an unprotected location or embedded in other text means the migration did
            // something to the protected value that this comparison cannot reproduce: never report it as equal.
            var restoredMarkers = new HashSet<string>(StringComparer.Ordinal);
            var restored = !content.Schema.HasSchemaMetadata() ? content.Content : await TryTransformForComparison(content.Schema, content.Content, (boundarySchema, _, node) =>
            {
                if (node is not JsonValue scalar || !scalar.TryGetValue<string>(out var marker) ||
                    !values.TryGetValue(marker, out var value) || !restoredMarkers.Add(marker))
                {
                    return Task.FromResult<JsonNode?>(null);
                }

                // Source values were protected before conversion. Migrated values, however, pass through
                // the target schema before protection; never restore raw plaintext over a lossy conversion.
                var restoredValue = value.DeepClone();
                return Task.FromResult(convertProtectedValue is null ? restoredValue : convertProtectedValue(generation, boundarySchema, restoredValue));
            });
            if (restored is null || VerificationMarkers.AppearIn(restored))
            {
                return null;
            }

            result[generation] = restored;
        }

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

    static ProtectionKeyLocation ProtectionLocation(string path, IJsonSchemaMetadataValueHandler[] handlers, string identifier)
    {
        if (handlers.Length != 1 || handlers[0].KeyLocationFor(identifier) is not { } location)
        {
            throw new ProtectedBoundaryUnsupported(path);
        }

        return location;
    }

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

    static bool CanInspectReferencedType(JsonSchema schema, int depth = 0)
    {
        // The strict type probe follows references recursively. Do not turn an unresolved or cyclic
        // definition into a new append failure while deciding whether to extend metadata reach.
        if (depth > 64) return false;
        if (schema.HasReference && !CanInspectReference(schema, depth)) return false;
        return schema.AllOf.Concat(schema.AnyOf).Concat(schema.OneOf).All(alternative => CanInspectReferencedType(alternative, depth + 1));
    }

    static bool CanInspectReference(JsonSchema schema, int depth) =>
        schema.Reference is { } reference && CanInspectReferencedType(reference, depth + 1);

    IJsonSchemaMetadataValueHandler[] RegisteredHandlers((SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)[] metadata) =>
        metadata.Where(_ => _propertyValueHandlers.ContainsKey((_.Category, _.Metadata.metadataType)))
            .Select(_ => _propertyValueHandlers[(_.Category, _.Metadata.metadataType)]).ToArray();

    async Task<JsonObject?> TryTransformForComparison(JsonSchema schema, JsonObject json, Func<JsonSchema, IJsonSchemaMetadataValueHandler, JsonNode, Task<JsonNode?>> transform)
    {
        var result = (JsonObject)json.DeepClone();
        try
        {
            // Reuse the append traversal and its protected boundaries, but do not apply display-release
            // fallbacks or provision keys. A failed boundary invalidates the complete comparison.
            await HandleActionFor(schema, string.Empty, result, SchemaMetadataActionFailed.ApplyAction, (_, _, node) => Task.FromResult(node), comparisonTransform: transform);
            return result;
        }
        catch (Exception)
        {
            return null;
        }
    }

    void ValidateComparisonBoundary(JsonSchema schema, (SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)[] metadata)
    {
        if (metadata.Length == 0)
        {
            return;
        }

        // Overlapping handlers have no proven inverse. Unknown handlers and ambiguous scalar kinds
        // must fail closed even when the normal append traversal would skip an unknown metadata type.
        if (metadata.Length != 1 || !_propertyValueHandlers.ContainsKey((metadata[0].Category, metadata[0].Metadata.metadataType)) ||
            !StrictJsonSchemaRelease.HasSupportedType(schema))
        {
            throw new InvalidOperationException("Protected boundary cannot be compared.");
        }
    }

    IEnumerable<(SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)> MetadataAcrossCategories(JsonSchema schema, bool forComparison = false)
    {
        var categories = (forComparison ? _categories.Concat([SchemaMetadataCategory.Compliance, SchemaMetadataCategory.Security]).Distinct() : _categories).ToArray();
        var own = categories.SelectMany(category => schema.GetSchemaMetadata(category).Select(metadata => (category, metadata)));
        if (!schema.HasReference || schema.Reference is not { } reference) return own;
        var referenced = categories.SelectMany(category => reference.GetSchemaMetadata(category).Select(metadata => (category, metadata))).ToArray();
        if (referenced.Length == 0) return own;
        if (CanInspectReferencedType(reference) && StrictJsonSchemaRelease.HasSupportedType(reference))
        {
            // Referenced containers retain their existing member-level protection boundaries.
            var type = reference.ActualTypeSchema.Type;
            return (type & (JsonObjectType.Object | JsonObjectType.Array)) != JsonObjectType.None ? own : own.Concat(referenced);
        }
        logger.UnsupportedReferencedMetadata();
        return own;
    }

    Task HandleActionFor(
        JsonSchema schema,
        string identifier,
        JsonObject json,
        string actionName,
        Func<IJsonSchemaMetadataValueHandler, string, JsonNode, Task<JsonNode>> action,
        string path = "",
        bool strictRelease = false,
        Func<JsonSchema, IJsonSchemaMetadataValueHandler, JsonNode, Task<JsonNode?>>? comparisonTransform = null,
        Func<string, IJsonSchemaMetadataValueHandler[], string, JsonNode, Task<JsonNode>>? boundaryTransform = null) =>
        WalkObject(new MetadataWalk { Identifier = identifier, ActionName = actionName, Action = action, StrictRelease = strictRelease, ComparisonTransform = comparisonTransform, BoundaryTransform = boundaryTransform }, schema, json, path);

    async Task WalkObject(MetadataWalk walk, JsonSchema schema, JsonObject json, string path)
    {
        var metadataForContainer = MetadataAcrossCategories(schema, walk.Comparing).ToArray();
        if (schema.Properties is null)
        {
            return;
        }

        foreach (var (property, value) in json.ToArray())
        {
            if (value is null)
            {
                continue;
            }

            var propertyPath = string.IsNullOrEmpty(path) ? property : $"{path}.{property}";
            var propertySchema = ResolvePropertySchema(walk, schema, property, propertyPath);
            var propertyMetadata = MetadataAcrossCategories(propertySchema, walk.Comparing).Concat(metadataForContainer).DistinctBy(_ => (_.Category, _.Metadata.metadataType)).ToArray();
            if (walk.Comparing)
            {
                ValidateComparisonBoundary(propertySchema, propertyMetadata);
            }

            if (walk.BoundaryTransform is not null && RegisteredHandlers(propertyMetadata) is { Length: > 0 } handlers)
            {
                json[property] = await walk.BoundaryTransform(propertyPath, handlers, walk.Identifier, value);
                continue;
            }

            var handlerApplied = false;
            foreach (var (category, metadata) in propertyMetadata)
            {
                if (!_propertyValueHandlers.TryGetValue((category, metadata.metadataType), out var handler))
                {
                    continue;
                }

                try
                {
                    var handled = await walk.Handle(handler, propertySchema, value, propertyPath);
                    json[property] = RestoreForAction(walk, handled, propertySchema);
                }
                catch (Exception ex)
                {
                    json[property] = DegradeOrThrow(walk, propertyPath, propertySchema, ex);
                }

                handlerApplied = true;
            }

            // Only descend when the property was not handled as a whole. A handled container has already
            // been replaced by its ciphertext, so recursing would mutate the detached original — the work
            // is thrown away on apply, and on release it would decrypt members that were never separately
            // encrypted.
            if (!handlerApplied && value is JsonObject or JsonArray)
            {
                await DescendIntoProperty(walk, propertySchema, value, propertyPath);
            }
        }
    }

    async Task WalkArray(MetadataWalk walk, JsonSchema arraySchema, JsonArray array, string path)
    {
        var itemSchema = arraySchema.Item?.ActualSchema;
        if (itemSchema is null)
        {
            return;
        }

        var itemMetadata = MetadataAcrossCategories(itemSchema, walk.Comparing).DistinctBy(_ => (_.Category, _.Metadata.metadataType)).ToArray();
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
                await WalkObject(walk, itemSchema, declaredObject, elementPath);
                continue;
            }

            if (walk.Comparing)
            {
                ValidateComparisonBoundary(itemSchema, itemMetadata);
            }

            if (walk.BoundaryTransform is not null && RegisteredHandlers(itemMetadata) is { Length: > 0 } handlers)
            {
                var transformed = await walk.BoundaryTransform(elementPath, handlers, walk.Identifier, element);
                if (!ReferenceEquals(transformed, element))
                {
                    array[i] = transformed;
                }

                continue;
            }

            if (!await WalkElementHandlers(walk, itemSchema, itemMetadata, array, i, elementPath))
            {
                // A classified element is protected as a whole, including collection-valued elements. Never
                // descend into the detached original or decrypt members that were not separately encrypted.
                await DescendIntoElement(walk, itemSchema, element, elementPath);
            }
        }
    }

    async Task<bool> WalkElementHandlers(
        MetadataWalk walk,
        JsonSchema itemSchema,
        (SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)[] itemMetadata,
        JsonArray array,
        int index,
        string elementPath)
    {
        var element = array[index]!;
        var handlerApplied = false;
        foreach (var (category, metadata) in itemMetadata)
        {
            if (!_propertyValueHandlers.TryGetValue((category, metadata.metadataType), out var handler))
            {
                continue;
            }

            try
            {
                var handled = await walk.Handle(handler, itemSchema, element, elementPath);
                var restored = RestoreForAction(walk, handled, itemSchema);
                if (!ReferenceEquals(restored, element))
                {
                    array[index] = restored;
                }
            }
            catch (Exception ex)
            {
                // Same asymmetry as the property walk — apply fails, release degrades the single element
                // so the rest of the array, and the query, still come back.
                array[index] = DegradeOrThrow(walk, elementPath, itemSchema, ex);
            }

            handlerApplied = true;
        }

        return handlerApplied;
    }

    Task DescendIntoProperty(MetadataWalk walk, JsonSchema propertySchema, JsonNode node, string path) =>
        node is JsonObject && propertySchema.DescribesGeospatialValue() ? Task.CompletedTask : DescendInto(walk, propertySchema.ActualTypeSchema, node, path);

    Task DescendIntoElement(MetadataWalk walk, JsonSchema itemSchema, JsonNode node, string path) =>
        node is JsonObject && itemSchema.DescribesGeospatialValue() ? Task.CompletedTask : DescendInto(walk, itemSchema, node, path);

    Task DescendInto(MetadataWalk walk, JsonSchema descentSchema, JsonNode node, string path) => node switch
    {
        // A geospatial value is an object on the wire but not a container (the callers skip it): the schema
        // emits it as a leaf, so there is no schema property under its GeoJSON members for a marker to sit on.
        JsonObject jsonObject => WalkObject(walk, descentSchema, jsonObject, path),

        // The array itself was not encrypted as a whole, so handle schema metadata that lives on the element
        // type — a [PII]/[Encrypted] scalar concept (e.g. IReadOnlyList<Email>) or a member marked that way.
        JsonArray jsonArray => WalkArray(walk, descentSchema, jsonArray, path),
        _ => Task.CompletedTask
    };

    JsonSchema ResolvePropertySchema(MetadataWalk walk, JsonSchema schema, string property, string propertyPath)
    {
        var flattenedProperties = schema.GetFlattenedProperties();

        // FirstOrDefault rather than Single: a schema flattened across inheritance can declare the same
        // property name more than once, and the duplicate is not a reason to fail the whole walk.
        return (JsonSchema?)flattenedProperties.FirstOrDefault(_ => _.Name == property) ??
            schema.AdditionalPropertiesSchema?.ActualSchema ??
            throw new SchemaPropertyNotFoundInSchema(walk.ActionName, propertyPath, walk.Identifier, flattenedProperties.Select(_ => _.Name));
    }

    JsonNode? RestoreForAction(MetadataWalk walk, JsonNode handled, JsonSchema schema) =>
        walk.ActionName == SchemaMetadataActionFailed.ReleaseAction ? RestoreReleasedContainerShape(handled, schema) : handled;

    JsonNode? DegradeOrThrow(MetadataWalk walk, string path, JsonSchema schema, Exception exception)
    {
        var failure = new SchemaMetadataActionFailed(walk.ActionName, path, walk.Identifier, exception);

        // Applying has to fail loudly — storing a value that was never protected is never
        // acceptable. Releasing must not: a single unreadable property is no reason to fail an
        // entire query, so surface it as empty — the shape an erased subject already produces
        // — and keep the diagnostic, which names the property, the identifier and the likely
        // cause, in the log.
        if (walk.ActionName != SchemaMetadataActionFailed.ReleaseAction || walk.StrictRelease)
        {
            throw failure;
        }

        logger.FailedToReleaseProperty(path, walk.Identifier, failure);
        return RestoreReleasedContainerShape(JsonValue.Create(string.Empty), schema);
    }

    readonly struct MetadataWalk
    {
        public required string Identifier { get; init; }

        public required string ActionName { get; init; }

        public required Func<IJsonSchemaMetadataValueHandler, string, JsonNode, Task<JsonNode>> Action { get; init; }

        public bool StrictRelease { get; init; }

        public Func<JsonSchema, IJsonSchemaMetadataValueHandler, JsonNode, Task<JsonNode?>>? ComparisonTransform { get; init; }

        public Func<string, IJsonSchemaMetadataValueHandler[], string, JsonNode, Task<JsonNode>>? BoundaryTransform { get; init; }

        public ErasedPathCollector? Erasure { get; init; }

        public bool Comparing => ComparisonTransform is not null;

        public Task<JsonNode> Handle(IJsonSchemaMetadataValueHandler handler, JsonSchema schema, JsonNode value, string path)
        {
            if (Erasure is not null)
            {
                return Erasure.Release(handler, Identifier, value, path);
            }

            return ComparisonTransform is null ? Action(handler, Identifier, value) : Compare(ComparisonTransform, handler, schema, value);
        }

        static async Task<JsonNode> Compare(
            Func<JsonSchema, IJsonSchemaMetadataValueHandler, JsonNode, Task<JsonNode?>> comparisonTransform,
            IJsonSchemaMetadataValueHandler handler,
            JsonSchema schema,
            JsonNode value) =>
            await comparisonTransform(schema, handler, value) ?? throw new InvalidOperationException("Protected value cannot be compared.");
    }
}
