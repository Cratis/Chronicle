// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Events.EventSequences.Migrations;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.EventTypes;

public sealed partial class EventTypeRegistrar
{
    static bool CanEvolve(EventTypeRegistration incoming, EventTypeDefinition? stored, bool enabled) =>
        enabled && incoming.Owner == Contracts.Events.EventTypeOwner.Client &&
        stored?.Owner == Concepts.Events.EventTypeOwner.Client;

    static async Task<string> ClassifyRegistration(EventTypeRegistration incoming, EventTypeDefinition? stored)
    {
        var classification = SchemaEvolutionKind.Identical;
        foreach (var generation in await ParseGenerations(incoming))
        {
            var existing = stored?.Generations.FirstOrDefault(_ => _.Generation == generation.Generation);
            if (existing is not null)
            {
                classification = (SchemaEvolutionKind)Math.Max((int)classification, (int)SchemaEvolution.Classify(existing.Schema, generation.Schema));
            }
        }

        return classification.ToString();
    }

    static async Task<EventTypeCheckResult> FlagAddedProtection(EventTypeCheckResult result, EventTypeRegistration incoming, EventTypeDefinition? stored)
    {
        foreach (var generation in await ParseGenerations(incoming))
        {
            var existing = stored?.Generations.FirstOrDefault(_ => _.Generation == generation.Generation);
            var path = existing?.Schema.AddedProtectionMetadataPaths(generation.Schema).FirstOrDefault();
            if (path is not null)
            {
                return result with
                {
                    Reason = "Protection was added to an existing property. Previously stored events are not retroactively protected.",
                    Keyword = "protection",
                    Path = path
                };
            }
        }

        return result;
    }

    static async Task<List<EventTypeToRegister>> EvolveExistingSchemas(
        List<EventTypeToRegister> registrations,
        Dictionary<EventTypeId, EventTypeDefinition> stored,
        IEventTypesStorage storage,
        Func<EventTypeSchemaEvolved, Task> onEvolution)
    {
        var result = new List<EventTypeToRegister>();
        foreach (var registration in registrations)
        {
            var previous = stored.GetValueOrDefault(registration.Definition.Id);
            if (previous?.Owner != Concepts.Events.EventTypeOwner.Client || registration.Definition.Owner != Concepts.Events.EventTypeOwner.Client)
            {
                result.Add(registration);
                continue;
            }

            var newGenerations = new List<Concepts.Events.EventTypeGenerationDefinition>();
            foreach (var generation in registration.Definition.Generations)
            {
                var existing = previous.Generations.FirstOrDefault(_ => _.Generation == generation.Generation);
                if (existing is null)
                {
                    newGenerations.Add(generation);
                    continue;
                }

                await EvolveGeneration(storage, registration.Definition.Id, (existing, generation), onEvolution);
            }

            // Existing generations have already been conditionally written (or left alone). Never send
            // them through the unconditional batch merge, including on an older client's reconnect.
            result.Add(registration with { Definition = registration.Definition with { Generations = newGenerations } });
        }

        return result;
    }

    static async Task EvolveGeneration(
        IEventTypesStorage storage,
        EventTypeId id,
        (Concepts.Events.EventTypeGenerationDefinition Stored, Concepts.Events.EventTypeGenerationDefinition Incoming) schemas,
        Func<EventTypeSchemaEvolved, Task> onEvolution)
    {
        var expected = schemas.Stored.Schema;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var classification = SchemaEvolution.Classify(expected, schemas.Incoming.Schema);
            if (classification is SchemaEvolutionKind.Subset ||
                JsonSchemaCompatibilityExtensions.EqualsIgnoringAnnotations(expected.ToJson(), schemas.Incoming.Schema.ToJson()))
            {
                return;
            }

            if (classification == SchemaEvolutionKind.Breaking)
            {
                throw new EventTypeSchemaChanged(id.Value, schemas.Incoming.Generation.Value);
            }

            var replacement = expected.MorePrecise(schemas.Incoming.Schema);
            if (await storage.TryReplaceSchema(id, schemas.Incoming.Generation, expected, replacement))
            {
                await onEvolution(new(id, schemas.Incoming.Generation, expected.ToJson(), replacement.ToJson(), classification.ToString()));

                return;
            }

            // A failed CAS is not success: re-read authoritative definitions, never the parsed-schema cache.
            var current = (await storage.GetAllDefinitions()).FirstOrDefault(_ => _.Id == id);
            var generation = current?.Generations.FirstOrDefault(_ => _.Generation == schemas.Incoming.Generation);
            if (current?.Owner != Concepts.Events.EventTypeOwner.Client || generation is null)
            {
                throw new EventTypeSchemaChanged(id.Value, schemas.Incoming.Generation.Value);
            }

            expected = generation.Schema;
        }

        throw new EventTypeSchemaChanged(id.Value, schemas.Incoming.Generation.Value);
    }
}
