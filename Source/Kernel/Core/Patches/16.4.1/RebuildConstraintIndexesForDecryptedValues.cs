// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Patching;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventStores;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Patching;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Naming;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Patches;

/// <summary>
/// Patch to rebuild unique constraint indexes so their values are hashed from the decrypted (plaintext) content.
/// </summary>
/// <remarks>
/// The original hashing rebuild (<see cref="RebuildConstraintIndexes"/>) hashed the stored content as-is, which for
/// a constraint covering a <c language="csharp">[PII]</c> property meant hashing the non-deterministic ciphertext — so two subjects
/// sharing the same value never collided and the uniqueness constraint silently did nothing. This patch reindexes
/// again, now decrypting PII before hashing, so PII-covering constraints are enforced correctly.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing storage.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
/// <param name="resourceNames">The optional <see cref="IResourceNameSnapshot"/> that tells which event stores are quarantined, and so are left out.</param>
[Singleton]
public class RebuildConstraintIndexesForDecryptedValues(IStorage storage, IGrainFactory grainFactory, ILogger<RebuildConstraintIndexesForDecryptedValues> logger, IResourceNameSnapshot? resourceNames = null) : ICanApplyPatch, IReportSkippedResources
{
    /// <inheritdoc/>
    public SkippedResources SkippedResources { get; private set; } = SkippedResources.None;

    /// <inheritdoc/>
    public SemanticVersion Version => new(16, 4, 1);

    /// <inheritdoc/>
    public async Task Up()
    {
        logger.StartingPatch();

        var eventStores = await UsableEventStores.From(storage, resourceNames, logger);
        SkippedResources = eventStores.SkippedResources;
        foreach (var eventStore in eventStores.Usable)
        {
            var eventStoreStorage = storage.GetEventStore(eventStore);
            var definitions = await eventStoreStorage.Constraints.GetDefinitions();
            var uniqueConstraintDefinitions = definitions.OfType<UniqueConstraintDefinition>().ToArray();

            if (uniqueConstraintDefinitions.Length == 0)
            {
                continue;
            }

            logger.FoundUniqueConstraints(eventStore, uniqueConstraintDefinitions.Length);

            var changes = uniqueConstraintDefinitions
                .Select(_ => new ConstraintDefinitionChange(_.Name, true, [ConstraintChangeType.IndexedPropertiesChanged]))
                .ToArray();

            var namespaces = grainFactory.GetGrain<INamespaces>(eventStore);
            var namespaceNames = await namespaces.GetAll();

            foreach (var namespaceName in namespaceNames)
            {
                logger.RebuildingConstraintIndexes(eventStore, namespaceName);

                var jobsManager = grainFactory.GetJobsManager(eventStore, namespaceName);
                var result = await jobsManager.Start<IReindexConstraints, ReindexConstraintsRequest>(new(EventSequenceId.Log, changes));
                if (result.TryGetError(out var error))
                {
                    logger.FailedRebuildingConstraintIndexes(eventStore, namespaceName, error.ToString());
                }
            }
        }

        logger.PatchCompleted();
    }
}
