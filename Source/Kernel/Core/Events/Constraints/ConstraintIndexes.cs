// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents an implementation of <see cref="IConstraintIndexes"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for getting the namespaces, event sequences and jobs managers.</param>
/// <param name="storage"><see cref="IStorage"/> for checking whether a namespace holds any data.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
/// <remarks>
/// Rebuilding is decided here, when the definitions are registered, rather than by each event sequence grain when it
/// notices the change. A grain only knows what changed while it is active and still holds the definitions it had
/// before, so a sequence that was inactive when a constraint was widened to cover it - the outbox after a redeploy,
/// for instance - would keep an index that is missing every value appended while it was not covered.
/// <para>
/// The event sequences of a namespace are those <see cref="IEventSequences"/> reports: the well-known sequences and
/// every other sequence that holds persisted state. A namespace that has never held any data has no index to rebuild
/// and is skipped.
/// </para>
/// </remarks>
[Singleton]
public class ConstraintIndexes(IGrainFactory grainFactory, IStorage storage, ILogger<ConstraintIndexes> logger) : IConstraintIndexes
{
    /// <inheritdoc/>
    public async Task RebuildStaleIndexes(
        EventStoreName eventStore,
        IReadOnlyCollection<IConstraintDefinition> previous,
        IReadOnlyCollection<IConstraintDefinition> current)
    {
        if (!current.OfType<UniqueConstraintDefinition>().Any())
        {
            return;
        }

        var namespaces = await grainFactory.GetGrain<INamespaces>(eventStore).GetAll();
        await Task.WhenAll(namespaces.Select(@namespace => RebuildStaleIndexes(eventStore, @namespace, previous, current)));
    }

    async Task RebuildStaleIndexes(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        IReadOnlyCollection<IConstraintDefinition> previous,
        IReadOnlyCollection<IConstraintDefinition> current)
    {
        try
        {
            if (!await storage.GetEventStore(eventStore).GetNamespace(@namespace).HasData())
            {
                return;
            }

            var eventSequences = await grainFactory.GetEventSequences(eventStore, @namespace).GetEventSequences();
            var jobsManager = grainFactory.GetJobsManager(eventStore, @namespace);
            foreach (var eventSequenceId in eventSequences)
            {
                var changes = ConstraintDefinitionComparison.GetReindexChanges(previous, current, eventSequenceId);
                if (changes.Count == 0)
                {
                    continue;
                }

                logger.StartingReindex(eventStore, @namespace, eventSequenceId);
                var result = await jobsManager.Start<IReindexConstraints, ReindexConstraintsRequest>(new(eventSequenceId, changes));
                if (result.TryGetError(out var error))
                {
                    logger.FailedStartingReindex(eventStore, @namespace, eventSequenceId, error.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            logger.FailedRebuildingStaleIndexes(eventStore, @namespace, ex);
        }
    }
}
