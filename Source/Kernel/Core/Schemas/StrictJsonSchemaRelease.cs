// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

                // Encryption represents every scalar as text. Restore only the protected value's
                // schema shape, never round-trip the document through a CLR event or drop members.
                if (released is JsonValue scalar && scalar.TryGetValue<string>(out var text) &&
                    !current.Type.HasFlag(JsonObjectType.String) &&
                    (current.IsArray || (current.Type & (JsonObjectType.Object | JsonObjectType.Integer | JsonObjectType.Number | JsonObjectType.Boolean)) != JsonObjectType.None))
                {
                    released = JsonNode.Parse(text);
                    if (released is null)
                    {
                        return (false, null);
                    }
                }

                return (true, released);
            }

            switch (node)
            {
                case JsonObject child when !current.DescribesGeospatialValue():
                    return (await ReleaseObject(current.ActualTypeSchema, child), child);
                case JsonArray array:
                    var item = current.ActualTypeSchema.Item?.ActualSchema;
                    if (item is null)
                    {
                        return (false, null);
                    }

                    for (var index = 0; index < array.Count; index++)
                    {
                        if (array[index] is not { } element)
                        {
                            continue;
                        }

                        var (success, released) = await ReleaseNode(item, element, []);
                        if (!success)
                        {
                            return (false, null);
                        }

                        array[index] = released;
                    }

                    break;
            }

            return (true, node);
        }
    }
}
