// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Determines whether an instance actually holds a value in a property that carries compliance or security metadata.
/// </summary>
/// <remarks>
/// A composed read model can declare a nullable <c language="csharp">[PII]</c> concept whose values are all null for
/// a given instance; there is then nothing to release, so a missing subject is not worth a warning.
/// </remarks>
static class SubjectScopedValuePresence
{
    const int MaxDepth = 32;

    /// <summary>
    /// Check whether the instance holds a non-null value where the schema carries metadata.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model.</typeparam>
    /// <param name="schema">The schema describing the instance.</param>
    /// <param name="instance">The instance to inspect.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> used to serialize the instance.</param>
    /// <returns>True if such a value exists, false if not.</returns>
    internal static bool HasValue<TReadModel>(JsonSchema schema, TReadModel instance, JsonSerializerOptions options) =>
        HasValue(schema, JsonSerializer.SerializeToNode(instance, options), 0);

    static bool HasValue(JsonSchema schema, JsonNode? node, int depth)
    {
        if (node is null || depth > MaxDepth)
        {
            return false;
        }

        var actual = schema.ActualSchema;
        if (actual.GetSchemaMetadata(SchemaMetadataCategory.Compliance).Any() ||
            actual.GetSchemaMetadata(SchemaMetadataCategory.Security).Any())
        {
            return true;
        }

        if (node is JsonObject obj)
        {
            return actual.GetFlattenedProperties().Any(property => HasValue(property, obj[property.Name], depth + 1));
        }

        return node is JsonArray array && actual.Item is not null && array.Any(element => HasValue(actual.Item, element, depth + 1));
    }
}
