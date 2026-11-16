// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Classifies conservative, in-place additions without weakening generation validation.
/// </summary>
public static class SchemaEvolution
{
    /// <summary>
    /// Classifies an incoming schema against a stored generation, including referenced definitions.
    /// </summary>
    /// <param name="stored">The registered schema.</param>
    /// <param name="incoming">The incoming schema.</param>
    /// <returns>The relationship between the schemas.</returns>
    public static SchemaEvolutionKind Classify(JsonSchema stored, JsonSchema incoming)
    {
        if (JsonSchemaCompatibilityExtensions.EqualsIgnoringAnnotations(stored.ToJson(), incoming.ToJson()))
        {
            return SchemaEvolutionKind.Identical;
        }

        if (stored.DifferingKeywords(stored.MorePrecise(incoming)) is null)
        {
            return SchemaEvolutionKind.Tolerated;
        }

        if (IsPropertySetChange(stored, incoming, false))
        {
            return SchemaEvolutionKind.Additive;
        }

        return IsPropertySetChange(incoming, stored, true) ? SchemaEvolutionKind.Subset : SchemaEvolutionKind.Breaking;
    }

    static bool IsPropertySetChange(JsonSchema stored, JsonSchema incoming, bool subset)
    {
        var original = JsonNode.Parse(stored.ToJson())!.AsObject();
        var candidate = JsonNode.Parse(incoming.ToJson())!.AsObject();
        var additions = PruneAdditions(original, candidate, !subset);
        if (additions is null or <= 0)
        {
            return false;
        }

        var pruned = JsonSchema.FromJson(candidate.ToJsonString());

        // For a subset the candidate is the stored superset: compare shared definitions in
        // the stored-to-client direction too, so omitted protection metadata is not tolerated.
        var (baseline, proposed) = subset ? (pruned, stored) : (stored, pruned);
        return baseline.DifferingKeywords(baseline.MorePrecise(proposed)) is null;
    }

    static int? PruneAdditions(JsonObject stored, JsonObject incoming, bool allowRequiredAdditions)
    {
        var additions = PruneProperties(stored, incoming, allowRequiredAdditions);
        foreach (var keyword in new[] { "$defs", "definitions" })
        {
            if (incoming[keyword] is not JsonObject definitions)
            {
                continue;
            }

            var storedDefinitions = stored[keyword] as JsonObject;
            additions += PruneDefinitions(storedDefinitions, definitions, allowRequiredAdditions);

            if (storedDefinitions is null && definitions.Count == 0)
            {
                incoming.Remove(keyword);
            }
        }

        return additions;
    }

    static int? PruneDefinitions(JsonObject? stored, JsonObject incoming, bool allowRequiredAdditions)
    {
        int? additions = 0;
        foreach (var definition in incoming.ToArray())
        {
            if (stored?[definition.Key] is JsonObject existing && definition.Value is JsonObject candidate)
            {
                additions += PruneAdditions(existing, candidate, allowRequiredAdditions);
            }
            else if (stored?.ContainsKey(definition.Key) != true)
            {
                incoming.Remove(definition.Key);
            }
        }

        return additions;
    }

    static int? PruneProperties(JsonObject stored, JsonObject incoming, bool allowRequiredAdditions)
    {
        if (incoming["properties"] is not JsonObject properties)
        {
            return 0;
        }

        var storedProperties = stored["properties"] as JsonObject;
        int? additions = 0;
        foreach (var property in properties.ToArray())
        {
            additions += PruneProperty(storedProperties, incoming, property, allowRequiredAdditions);
        }

        if (storedProperties is null && properties.Count == 0)
        {
            incoming.Remove("properties");
        }

        if (stored["required"] is null && incoming["required"] is JsonArray { Count: 0 })
        {
            incoming.Remove("required");
        }

        return additions;
    }

    static int? PruneProperty(JsonObject? stored, JsonObject incoming, KeyValuePair<string, JsonNode?> property, bool allowRequiredAdditions)
    {
        if (stored?.ContainsKey(property.Key) == true)
        {
            return stored[property.Key] is JsonObject existing && property.Value is JsonObject candidate
                ? PruneAdditions(existing, candidate, allowRequiredAdditions) : 0;
        }

        if (IsRequired(incoming, property.Key) && !RemoveSafeRequiredAddition(incoming, property, allowRequiredAdditions))
        {
            return null;
        }

        incoming["properties"]!.AsObject().Remove(property.Key);
        return 1;
    }

    static bool RemoveSafeRequiredAddition(JsonObject schema, KeyValuePair<string, JsonNode?> property, bool allowed)
    {
        if (!allowed || property.Value is not JsonObject definition)
        {
            return false;
        }

        var nullable = definition["type"] is JsonArray types && types.Any(_ => _?.GetValue<string>() == "null");
        if (!definition.ContainsKey("default") && !nullable)
        {
            return false;
        }

        var required = schema["required"]!.AsArray();
        required.Remove(required.First(_ => _?.GetValue<string>() == property.Key));
        return true;
    }

    static bool IsRequired(JsonObject schema, string property) =>
        schema["required"] is JsonArray required && required.Any(_ => _?.GetValue<string>() == property);
}
