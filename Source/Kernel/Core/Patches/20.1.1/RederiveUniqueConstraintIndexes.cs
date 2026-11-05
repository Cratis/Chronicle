// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Patching;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Patches;

/// <summary>
/// Rebuilds the unique constraint indexes of every event sequence that holds claims whose owner is not known exactly.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for accessing storage.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
public class RederiveUniqueConstraintIndexes(IStorage storage, IGrainFactory grainFactory, ILogger<RederiveUniqueConstraintIndexes> logger) : ICanApplyPatch
{
    /// <summary>
    /// The version of the patch. A placeholder that is set when the release train is cut. It must stay above the
    /// version that the previous release recorded, since a patch is only selected when its version is above it.
    /// </summary>
    public static readonly SemanticVersion PatchVersion = new(20, 1, 1);

    /// <inheritdoc/>
    public SemanticVersion Version => PatchVersion;

    /// <inheritdoc/>
    public async Task Up()
    {
        foreach (var eventStore in await storage.GetEventStores())
        {
            var eventStoreStorage = storage.GetEventStore(eventStore);
            var definitions = (await eventStoreStorage.Constraints.GetDefinitions()).OfType<UniqueConstraintDefinition>().ToArray();
            if (definitions.Length == 0)
            {
                continue;
            }

            var namespaces = await grainFactory.GetGrain<INamespaces>(eventStore).GetAll();
            foreach (var namespaceName in namespaces)
            {
                await Rederive(eventStore, namespaceName, eventStoreStorage, definitions);
            }
        }

        logger.PatchCompleted();
    }

    async Task Rederive(EventStoreName eventStore, EventStoreNamespaceName namespaceName, IEventStoreStorage eventStoreStorage, UniqueConstraintDefinition[] definitions)
    {
        var sequences = await grainFactory.GetEventSequences(eventStore, namespaceName).GetEventSequences();
        var namespaceStorage = eventStoreStorage.GetNamespace(namespaceName);
        foreach (var sequence in sequences.Append(EventSequenceId.Log).Distinct())
        {
            var pending = await namespaceStorage.GetUniqueConstraintsStorage(sequence).CountPendingSpellings();
            if (pending == 0)
            {
                continue;
            }

            var changes = definitions
                .Where(definition => definition.AppliesTo(sequence))
                .Select(definition => new ConstraintDefinitionChange(definition.Name, true, [ConstraintChangeType.IndexedPropertiesChanged]))
                .ToArray();
            if (changes.Length == 0)
            {
                continue;
            }

            logger.RederivingConstraintIndexes(eventStore, namespaceName, sequence, pending);
            var result = await grainFactory.GetJobsManager(eventStore, namespaceName).Start<IReindexConstraints, ReindexConstraintsRequest>(new(sequence, changes));
            if (result.TryGetError(out var error))
            {
                logger.FailedRederivingConstraintIndexes(eventStore, namespaceName, sequence, error.ToString());
            }
        }
    }
}
