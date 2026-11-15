// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Compares an incoming registration's schemas with the ones already stored.
/// </summary>
static class EventTypeSchemaComparison
{
    /// <summary>
    /// Finds the first stored generation whose schema the registration would change.
    /// </summary>
    /// <param name="eventType">The incoming registration.</param>
    /// <param name="storedDefinition">The stored definition, if any.</param>
    /// <returns>The <see cref="SchemaProblem"/>, or <see langword="null"/> when nothing stored would change.</returns>
    internal static async Task<SchemaProblem?> FindProblem(EventTypeRegistration eventType, Concepts.Events.EventTypeDefinition? storedDefinition)
    {
        if (storedDefinition is null)
        {
            return null;
        }

        var definitions = eventType.Generations.Count > 0 ? eventType.Generations :
            [new Contracts.Events.EventTypeGenerationDefinition { Generation = eventType.Type.Generation, Schema = eventType.Schema }];
        foreach (var genDef in definitions)
        {
            if (await FindDifferenceInGeneration(storedDefinition, genDef) is { } problem)
            {
                return problem;
            }
        }

        return null;
    }

    static async Task<SchemaProblem?> FindDifferenceInGeneration(Concepts.Events.EventTypeDefinition storedDefinition, Contracts.Events.EventTypeGenerationDefinition genDef)
    {
        var generation = new EventTypeGeneration(genDef.Generation);
        var existingGeneration = storedDefinition.Generations.FirstOrDefault(_ => _.Generation == generation);
        if (existingGeneration is null)
        {
            return null;
        }

        var newSchema = await JsonSchema.FromJsonAsync(genDef.Schema);

        // Storage applies EnsureComplianceMetadata() when deserializing a stored schema
        // (in EventTypeConverters.ToDefinition). Apply the same transformation to the incoming
        // schema so both sides go through identical normalization before comparison.
        newSchema.EnsureComplianceMetadata();

        // Compare for compatibility rather than equality. Two differences cannot change what an already
        // stored payload means and must not be rejected: a nullability marker ('?' on a format value), which
        // a Chronicle upgrade can introduce on a schema stored before the marker existed, and an enumeration
        // that only gained members or had members renamed - neither moves an existing member off the
        // underlying value a stored payload carries. Everything else, including a member that disappeared or
        // was renumbered, still needs a new generation.
        var refined = existingGeneration.Schema.MorePrecise(newSchema);
        return existingGeneration.Schema.DifferingKeywords(refined) is { } difference
            ? new SchemaProblem(genDef.Generation, difference)
            : null;
    }
}
