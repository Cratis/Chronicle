// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Releases a complete document without the empty-value fallbacks used by display reads.
/// </summary>
/// <param name="handlers">The available metadata handlers.</param>
internal sealed class StrictJsonSchemaRelease(IReadOnlyDictionary<(SchemaMetadataCategory Category, SchemaMetadataTypeName Type), IJsonSchemaMetadataValueHandler> handlers)
{
    readonly SchemaMetadataCategory[] _categories = [SchemaMetadataCategory.Compliance, SchemaMetadataCategory.Security, .. handlers.Keys.Select(_ => _.Category).Distinct()];

    /// <summary>
    /// Attempts to release every protected value without discarding document members.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="schema">The generation schema.</param>
    /// <param name="identifier">The protection identifier.</param>
    /// <param name="content">The stored document.</param>
    /// <returns>The complete released document, or null.</returns>
    internal async Task<JsonObject?> Release(EventStoreName eventStore, EventStoreNamespaceName @namespace, JsonSchema schema, string identifier, JsonObject content)
    {
        var result = (JsonObject)content.DeepClone();
        return await ReleaseObject(schema, result) ? result : null;

        IEnumerable<(SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)> Metadata(JsonSchema current) =>
            _categories.Distinct().SelectMany(category => current.GetSchemaMetadata(category).Select(metadata => (category, metadata)));

        async Task<bool> ReleaseObject(JsonSchema current, JsonObject value)
        {
            var properties = current.GetFlattenedProperties().ToArray();
            foreach (var (name, node) in value.ToArray())
            {
                if (node is null)
                {
                    continue;
                }

                var property = properties.FirstOrDefault(_ => _.Name == name);
                if (property is null)
                {
                    // No schema means we cannot establish whether this value requires release.
                    return false;
                }

                var (success, released) = await ReleaseNode(property, node, Metadata(current));
                if (!success)
                {
                    return false;
                }

                value[name] = released;
            }

            return true;
        }

        async Task<(bool Success, JsonNode? Value)> ReleaseNode(JsonSchema current, JsonNode node, IEnumerable<(SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)> inherited)
        {
            var metadata = Metadata(current).Concat(inherited).DistinctBy(_ => (_.Category, _.Metadata.metadataType)).ToArray();
            if (metadata.Length > 1)
            {
                // Multiple handlers can overwrite one another on apply; there is no proven inverse.
                return (false, null);
            }

            if (metadata.Length == 1)
            {
                var type = ProtectedType(current);
                if (type is not (JsonObjectType.String or JsonObjectType.Integer or JsonObjectType.Number or JsonObjectType.Boolean or JsonObjectType.Array or JsonObjectType.Object))
                {
                    // The codec encrypts ToString(), not typed JSON. Without exactly one non-null
                    // type, a decrypted "42" cannot tell us whether the original was text or a number.
                    return (false, null);
                }

                var entry = metadata[0];
                if (!handlers.TryGetValue((entry.Category, entry.Metadata.metadataType), out var handler))
                {
                    return (false, null);
                }

                var released = await handler.TryRelease(eventStore, @namespace, identifier, node);
                if (released is null)
                {
                    return (false, null);
                }

                if (type != JsonObjectType.String && released is JsonValue scalar && scalar.TryGetValue<string>(out var text))
                {
                    released = JsonNode.Parse(text);
                }

                var valid = type switch
                {
                    JsonObjectType.String => released?.GetValueKind() == JsonValueKind.String,
                    JsonObjectType.Boolean => released?.GetValueKind() is JsonValueKind.True or JsonValueKind.False,
                    JsonObjectType.Integer or JsonObjectType.Number => released?.GetValueKind() == JsonValueKind.Number,
                    JsonObjectType.Array => released is JsonArray,
                    JsonObjectType.Object => released is JsonObject,
                    _ => false
                };
                return (valid, released);
            }

            switch (node)
            {
                case JsonObject child when !current.DescribesGeospatialValue():
                    return (await ReleaseObject(current.ActualTypeSchema, child), child);
                case JsonArray array:
                    return (await ReleaseArray(current.ActualTypeSchema, array), array);
            }

            return (true, node);
        }

        async Task<bool> ReleaseArray(JsonSchema current, JsonArray array)
        {
            var item = current.Item?.ActualSchema;
            if (item is null)
            {
                return false;
            }

            for (var index = 0; index < array.Count; index++)
            {
                if (array[index] is not { } element)
                {
                    continue;
                }

                // Mirror JsonSchemaMetadataManager.HandleActionForArray: object-level metadata
                // protects each member, not the whole element. Nested arrays also descend first.
                switch (element)
                {
                    case JsonObject child when !item.DescribesGeospatialValue():
                        if (!await ReleaseObject(item, child))
                        {
                            return false;
                        }

                        break;
                    case JsonArray nested:
                        if (!await ReleaseArray(item, nested))
                        {
                            return false;
                        }

                        break;
                    default:
                        var (success, released) = await ReleaseNode(current.Item!, element, []);
                        if (!success)
                        {
                            return false;
                        }

                        array[index] = released;
                        break;
                }
            }

            return true;
        }
    }

    static JsonObjectType ProtectedType(JsonSchema schema)
    {
        // Do not use ActualTypeSchema here: for a union it selects the first non-null branch,
        // which would hide exactly the ambiguity this operation must reject.
        var type = schema.Type;
        if (schema.HasReference)
        {
            type |= ProtectedType(schema.Reference!);
        }

        foreach (var alternative in schema.OneOf.Concat(schema.AnyOf).Concat(schema.AllOf))
        {
            var alternativeType = ProtectedType(alternative);
            if (alternativeType == JsonObjectType.None && alternative.Type != JsonObjectType.Null)
            {
                return JsonObjectType.None;
            }

            type |= alternativeType;
        }

        return type & ~JsonObjectType.Null;
    }
}
