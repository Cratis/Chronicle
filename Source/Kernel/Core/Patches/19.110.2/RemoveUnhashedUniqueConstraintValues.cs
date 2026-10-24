// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Patching;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Patches;

/// <summary>
/// Deletes unhashed unique-constraint claims and directly repairs SQL value columns and indexes.
/// </summary>
/// <param name="storage">The storage to clean.</param>
/// <param name="grainFactory">The grain factory for namespace and sequence discovery.</param>
/// <param name="logger">The logger.</param>
public class RemoveUnhashedUniqueConstraintValues(IStorage storage, IGrainFactory grainFactory, ILogger<RemoveUnhashedUniqueConstraintValues> logger) : ICanApplyPatch
{
    /// <inheritdoc/>
    public SemanticVersion Version => new(19, 110, 2);

    /// <inheritdoc/>
    public async Task Up()
    {
        foreach (var eventStore in await storage.GetEventStores())
        {
            var eventStoreStorage = storage.GetEventStore(eventStore);
            var namespaces = await grainFactory.GetGrain<INamespaces>(eventStore).GetAll();
            foreach (var namespaceName in namespaces)
            {
                var sequences = await grainFactory.GetEventSequences(eventStore, namespaceName).GetEventSequences();
                var namespaceStorage = eventStoreStorage.GetNamespace(namespaceName);
                foreach (var sequence in sequences.Append(EventSequenceId.Log).Distinct())
                {
                    var counts = await namespaceStorage.GetUniqueConstraintsStorage(sequence).RemoveUnhashedValues();
                    logger.RemovedUnhashedValues(eventStore, namespaceName, sequence, counts.PerEventSource, counts.PerValue);
                }
            }
        }
    }

    /// <inheritdoc/>
    public Task Down()
    {
        logger.RestoreBackupRequired();
        return Task.CompletedTask;
    }
}
