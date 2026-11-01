// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Sql.EventStores.Occurrences;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverSequenceCheckpoints;

/// <summary>
/// Represents SQL storage of sequence-local observer checkpoints.
/// </summary>
/// <param name="eventStore">The event store.</param>
/// <param name="namespace">The namespace.</param>
/// <param name="database">The database.</param>
public class ObserverSequenceCheckpointsStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, IDatabase database) : IObserverSequenceCheckpointsStorage
{
    /// <inheritdoc/>
    public async Task<ObserverSequenceCheckpoint?> Get(ObserverId observerId, EventSequenceId eventSequenceId)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var observer = observerId.Value;
        var sequence = eventSequenceId.Value;
        var entity = await scope.DbContext.ObserverSequenceCheckpoints.AsNoTracking()
            .SingleOrDefaultAsync(row => row.ObserverId == observer && row.EventSequenceId == sequence);
        return entity?.ToKernel();
    }

    /// <inheritdoc/>
    public async Task Save(ObserverSequenceCheckpoint checkpoint)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var entity = checkpoint.ToEntity();
        var existing = await scope.DbContext.ObserverSequenceCheckpoints.AsTracking()
            .SingleOrDefaultAsync(row => row.ObserverId == entity.ObserverId && row.EventSequenceId == entity.EventSequenceId);
        if (existing is null)
        {
            scope.DbContext.ObserverSequenceCheckpoints.Add(entity);
        }
        else
        {
            scope.DbContext.Entry(existing).CurrentValues.SetValues(entity);
        }

        try
        {
            await scope.DbContext.SaveChangesAsync();
        }
        catch (DbUpdateException error) when (existing is null && StorageWriteConflicts.IsUnique(error))
        {
            // Another writer inserted the same key. Replace its checkpoint, just like an ordinary upsert.
            scope.DbContext.ChangeTracker.Clear();
            var winner = await scope.DbContext.ObserverSequenceCheckpoints.AsTracking()
                .SingleOrDefaultAsync(row => row.ObserverId == entity.ObserverId && row.EventSequenceId == entity.EventSequenceId);
            if (winner is null) throw;
            scope.DbContext.Entry(winner).CurrentValues.SetValues(entity);
            await scope.DbContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc/>
    public async Task RemoveAllFor(ObserverId observerId)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var observer = observerId.Value;
        await scope.DbContext.ObserverSequenceCheckpoints.Where(row => row.ObserverId == observer).ExecuteDeleteAsync();
    }
}
