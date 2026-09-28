// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReplayedModels;

/// <summary>
/// Represents an implementation of <see cref="IReplayedReadModelsStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="namespace">The name of the namespace.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class ReplayedModelsStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, IDatabase database) : IReplayedReadModelsStorage
{
    /// <inheritdoc/>
    public async Task Replayed(ReadModelOccurrence occurrence)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);

        var entry = ReplayedModelsConverters.ToReplayedModelOccurrence(occurrence);

        // Recording the same replay again - a retried grain state write - updates the row it already has.
        // Matched in memory because not every provider translates DateTimeOffset comparisons.
        var existing = (await scope.DbContext.ReplayedModels
            .Where(_ => _.ObserverId == entry.ObserverId)
            .ToListAsync())
            .FirstOrDefault(_ => _.Started == entry.Started);
        if (existing is null)
        {
            scope.DbContext.ReplayedModels.Add(entry);
        }
        else
        {
            existing.ReadModelIdentifier = entry.ReadModelIdentifier;
            existing.Generation = entry.Generation;
            existing.ReadModelName = entry.ReadModelName;
            existing.RevertModelName = entry.RevertModelName;
        }

        await scope.DbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ReadModelOccurrence>> GetOccurrences(ReadModelIdentifier readModel)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);

        var occurrences = await scope.DbContext.ReplayedModels
            .Where(r => r.ReadModelIdentifier == readModel.Value)
            .ToListAsync();

        return occurrences.Select(ReplayedModelsConverters.ToReadModelOccurrence);
    }

    /// <inheritdoc/>
    public async Task Remove(ReadModelOccurrence occurrence)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);

        var observerId = occurrence.ObserverId.Value;
        var readModel = occurrence.Type.Identifier.Value;
        var candidates = await scope.DbContext.ReplayedModels
            .Where(_ => _.ObserverId == observerId && _.ReadModelIdentifier == readModel)
            .ToListAsync();

        // An observer has one row per replay; remove only the replay this occurrence describes.
        // Matched in memory because not every provider translates DateTimeOffset comparisons, and at the
        // precision the row was stored with, since the occurrence may come from memory rather than storage.
        var started = ReplayedModelsConverters.ToStoredPrecision(occurrence.Occurred);
        var replayedModels = candidates
            .Where(_ => ReplayedModelsConverters.ToStoredPrecision(_.Started) == started && _.RevertModelName == occurrence.RevertContainerName.Value)
            .ToArray();
        if (replayedModels.Length == 0)
        {
            return;
        }

        scope.DbContext.ReplayedModels.RemoveRange(replayedModels);
        await scope.DbContext.SaveChangesAsync();
    }
}
