// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Migrations;

/// <summary>
/// Resolves protection inherited by plain-string properties from higher generations without changing registered schemas.
/// </summary>
internal static class GenerationProtectionSchemas
{
    static readonly ConditionalWeakTable<JsonSchema, Entry> _cache = new();

    /// <summary>
    /// Resolves a generation's protection from a definition snapshot.
    /// </summary>
    /// <param name="definition">The definition containing registered generation schemas.</param>
    /// <param name="generation">The generation to protect or release.</param>
    /// <returns>The registered schema or an immutable clone with inherited metadata.</returns>
    internal static JsonSchema For(EventTypeDefinition definition, EventTypeGeneration generation) =>
        For(generation, definition.Generations.Select(_ => (_.Generation, _.Schema)));

    /// <summary>
    /// Resolves a generation's protection using the nearest higher classification at each matching plain-string path.
    /// </summary>
    /// <param name="generation">The generation to protect or release.</param>
    /// <param name="generations">The available generations and their registered schemas.</param>
    /// <returns>The registered schema when unchanged; otherwise a cached clone with inherited metadata.</returns>
    internal static JsonSchema For(EventTypeGeneration generation, IEnumerable<(EventTypeGeneration Generation, JsonSchema Schema)> generations)
    {
        var available = generations.ToArray();
        var own = available.Single(_ => _.Generation == generation).Schema;
        var higher = available.Where(_ => _.Generation > generation).OrderBy(_ => _.Generation).Select(_ => _.Schema).ToArray();
        if (_cache.TryGetValue(own, out var entry) && entry.Higher.SequenceEqual(higher, ReferenceEqualityComparer.Instance))
        {
            return entry.Effective;
        }

        var effective = CreateEffective(own, higher);
        _cache.AddOrUpdate(own, new(higher, effective));
        return effective;
    }

    static JsonSchema CreateEffective(JsonSchema own, JsonSchema[] higher)
    {
        if (!higher.Any(schema => MatchingProperties(own, schema, []).Any(pair => CanInherit(pair.Target, pair.Higher))))
        {
            return own;
        }

        var effective = JsonSchema.FromJson(own.ToJson());
        foreach (var schema in higher)
        {
            foreach (var (target, source) in MatchingProperties(effective, schema, []).Where(pair => CanInherit(pair.Target, pair.Higher)))
            {
                CopyMetadata(target, source, SchemaMetadataCategory.Compliance, ComplianceJsonSchemaExtensions.ComplianceKey);
                CopyMetadata(target, source, SchemaMetadataCategory.Security, SecurityJsonSchemaExtensions.SecurityKey);
            }
        }

        // Finish mutations before any cached HasSchemaMetadata query. A shared $ref definition may
        // protect every use of that definition in this clone; that conservative over-protection is intentional.
        return effective;
    }

    static bool CanInherit(JsonSchema target, JsonSchema higher)
    {
        var actual = target.ActualTypeSchema;
        const JsonObjectType nonString = JsonObjectType.Integer | JsonObjectType.Number | JsonObjectType.Boolean | JsonObjectType.Object | JsonObjectType.Array;
        return actual.Type.HasFlag(JsonObjectType.String) && (actual.Type & nonString) == JsonObjectType.None && string.IsNullOrEmpty(actual.Format) &&
            !HasOwnMetadata(target) && HasOwnMetadata(higher);
    }

    static bool HasOwnMetadata(JsonSchema schema) =>
        schema.GetSchemaMetadata(SchemaMetadataCategory.Compliance).Any() || schema.GetSchemaMetadata(SchemaMetadataCategory.Security).Any();

    static void CopyMetadata(JsonSchema target, JsonSchema higher, SchemaMetadataCategory category, string key)
    {
        var metadata = higher.GetSchemaMetadata(category).ToArray();
        if (metadata.Length > 0)
        {
            target.ExtensionData![key] = metadata;
        }
    }

    static IEnumerable<(JsonSchema Target, JsonSchema Higher)> MatchingProperties(JsonSchema target, JsonSchema higher, HashSet<(JsonSchema Target, JsonSchema Higher)> visited)
    {
        target = target.ActualTypeSchema;
        higher = higher.ActualTypeSchema;
        if (!visited.Add((target, higher)))
        {
            yield break;
        }

        var higherProperties = higher.GetFlattenedProperties().DistinctBy(_ => _.Name).ToDictionary(_ => _.Name);
        foreach (var property in target.GetFlattenedProperties().Where(_ => higherProperties.ContainsKey(_.Name)))
        {
            var source = higherProperties[property.Name];
            yield return (property, source);
            foreach (var pair in MatchingProperties(property, source, visited))
            {
                yield return pair;
            }
        }

        foreach (var pair in MatchingItemProperties(target.Item?.ActualSchema, higher.Item?.ActualSchema, visited))
        {
            yield return pair;
        }
    }

    static IEnumerable<(JsonSchema Target, JsonSchema Higher)> MatchingItemProperties(JsonSchema? target, JsonSchema? higher, HashSet<(JsonSchema Target, JsonSchema Higher)> visited)
    {
        if (target?.Type.HasFlag(JsonObjectType.Object) != true || higher?.Type.HasFlag(JsonObjectType.Object) != true)
        {
            return [];
        }

        return MatchingProperties(target, higher, visited);
    }

    sealed record Entry(JsonSchema[] Higher, JsonSchema Effective);
}
