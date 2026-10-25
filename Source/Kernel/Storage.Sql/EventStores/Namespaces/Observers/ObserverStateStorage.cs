// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Sql.EventStores.Occurrences;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers;

/// <summary>
/// Represents an implementation of <see cref="IObserverStateStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="namespace">The name of the namespace.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class ObserverStateStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, IDatabase database) : IObserverStateStorage
{
    /// <inheritdoc/>
    public async Task<IEnumerable<Observation.ObserverState>> GetAll()
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var observers = await scope.DbContext.Observers.ToListAsync();
        return observers.Select(observer => observer.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<Observation.ObserverState>> ObserveAll() => LiveQuery.Observe(GetAll, database, "observer-states");

    /// <inheritdoc/>
    public async Task<Observation.ObserverState> Get(ObserverId observerId)
    {
        // KeyHelper.Parse decodes an empty leading segment of a grain key as a null ObserverId
        // (the constructor accepts it as null!). The MongoDB sink quietly returns Empty in that
        // case because Match on null matches nothing; the SQL path used to NRE on observerId.Value
        // and abort grain activation. Match the MongoDB contract here so storage choice never
        // changes whether a grain can re-activate.
        if (observerId is null || string.IsNullOrEmpty(observerId.Value))
        {
            return new Observation.ObserverState();
        }

        await using var scope = await database.Namespace(eventStore, @namespace);
        var observerIdValue = observerId.Value;
        return await scope.DbContext.Observers
            .Where(observer => observer.Id == observerIdValue)
            .Select(observer => observer.ToKernel())
            .FirstOrDefaultAsync() ?? new Observation.ObserverState();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ObserverId>> GetRetired(IEnumerable<ObserverId> observerIds)
    {
        var ids = observerIds.Distinct().ToArray();
        if (ids.Length == 0) return [];

        await using var scope = await database.Namespace(eventStore, @namespace);

        return await scope.DbContext.Observers
            .Where(observer => ids.Contains(observer.Id) && observer.AlertDisposition == AlertDisposition.Retired)
            .Select(observer => observer.Id)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> Save(Observation.ObserverState state, EventSequenceId owner)
    {
        // EF change-tracking rejects a null primary key. Mirror the Get path tolerance: an
        // observer whose grain key decoded to an empty ObserverId has no real identity to
        // persist, and MongoDB's ReplaceOneAsync(IsUpsert: true) silently accepts the same
        // input. Returning here keeps storage-layer behavior aligned across backends.
        if (state.Identifier is null || string.IsNullOrEmpty(state.Identifier.Value))
        {
            return true;
        }

        await using var scope = await database.Namespace(eventStore, @namespace);
        var entity = state.ToSql();
        entity.Owner = owner.Value;
        var existing = await scope.DbContext.Observers.AsTracking()
            .FirstOrDefaultAsync(observer => observer.Id == entity.Id);
        try
        {
            return await SaveState(scope.DbContext, entity, existing);
        }
        catch (DbUpdateException error) when (existing is null && StorageWriteConflicts.IsUnique(error))
        {
            scope.DbContext.ChangeTracker.Clear();
            var winner = await scope.DbContext.Observers.AsTracking().FirstOrDefaultAsync(observer => observer.Id == entity.Id);
            if (winner is null)
            {
                throw;
            }

            SqlStorageTelemetry.RecoveredCollision("observer-state");
            return await SaveState(scope.DbContext, entity, winner);
        }
    }

    /// <inheritdoc/>
    public async Task ClaimOwnership(ObserverId observerId, EventSequenceId owner)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        await scope.DbContext.Observers.Where(observer => observer.Id == observerId)
            .ExecuteUpdateAsync(set => set.SetProperty(observer => observer.Owner, owner.Value));
    }

    /// <inheritdoc/>
    public async Task Delete(ObserverId observerId)
    {
        if (observerId is null || string.IsNullOrEmpty(observerId.Value))
        {
            return;
        }

        await using var scope = await database.Namespace(eventStore, @namespace);
        var observerIdValue = observerId.Value;
        var existing = await scope.DbContext.Observers.FirstOrDefaultAsync(observer => observer.Id == observerIdValue);
        if (existing is null)
        {
            return;
        }

        scope.DbContext.Observers.Remove(existing);
        await scope.DbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task Rename(ObserverId currentId, ObserverId newId)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var existing = await scope.DbContext.Observers.FindAsync(currentId);
        if (existing is null || currentId == newId)
        {
            return;
        }

        var renamed = new ObserverState
        {
            Id = newId,
            Owner = existing.Owner,
            TypeSetControl = existing.TypeSetControl,
            Initialization = existing.Initialization,
            DeliveryFences = existing.DeliveryFences,
            AlertLifecycleId = existing.AlertLifecycleId,
            AlertRevision = existing.AlertRevision,
            AlertDisposition = existing.AlertDisposition,
            QuarantineEpisodeId = existing.QuarantineEpisodeId,
            NextEventSequenceNumber = existing.NextEventSequenceNumber,
            SubscribesToAllEvents = existing.SubscribesToAllEvents,
            LastHandledEventSequenceNumber = existing.LastHandledEventSequenceNumber,
            TailEventSequenceNumber = existing.TailEventSequenceNumber,
            RunningState = existing.RunningState,
            ReplayingPartitions = existing.ReplayingPartitions,
            CatchingUpPartitions = existing.CatchingUpPartitions,
            FailedPartitions = existing.FailedPartitions,
            FailedPartitionCount = existing.FailedPartitionCount,
            IsReplaying = existing.IsReplaying,
            HandledEventCount = existing.HandledEventCount,
            HandledEventCountPerEventType = existing.HandledEventCountPerEventType
        };

        try
        {
            // A user-initiated transaction must run inside the execution strategy, since SQL Server and
            // PostgreSQL use retrying strategies that refuse transactions started outside them.
            await scope.DbContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                scope.DbContext.ChangeTracker.Clear();
                await using var transaction = await scope.DbContext.Database.BeginTransactionAsync();
                await scope.DbContext.Observers.AddAsync(renamed);
                await scope.DbContext.SaveChangesAsync();
                await scope.DbContext.Observers.Where(observer => observer.Id == currentId).ExecuteDeleteAsync();
                await transaction.CommitAsync();
            });
        }
        catch (DbUpdateException)
        {
            // The transaction is rolled back on dispose (PostgreSQL aborts it on a unique violation).
            // Converge only when a peer completed this same rename: the source row is gone.
            await using var fresh = await database.Namespace(eventStore, @namespace);
            if (await fresh.DbContext.Observers.AnyAsync(observer => observer.Id == currentId))
            {
                throw;
            }
        }
    }

    static async Task<bool> SaveState(NamespaceDbContext context, ObserverState entity, ObserverState? existing)
    {
        if (existing is null)
        {
            await context.Observers.AddAsync(entity);
        }
        else
        {
            if (existing.Owner is not null && existing.Owner != entity.Owner) return false;
            PreserveControl(entity, existing);

            // Owner is a concurrency token: the UPDATE atomically requires the read owner (this sequence or null).
            // Keep the existing control token as well, so competing control writes still fail their concurrency check.
            context.Entry(existing).CurrentValues.SetValues(entity);
        }

        try
        {
            await context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException error)
        {
            var entry = error.Entries.Single();
            var current = await entry.GetDatabaseValuesAsync();
            if (current is null || !Equals(current[nameof(ObserverState.Owner)], entry.OriginalValues[nameof(ObserverState.Owner)])) return false;
            throw;
        }
    }

    static void PreserveControl(ObserverState entity, ObserverState existing)
    {
        // On an insert collision the progress fields take the losing writer's values, matching
        // MongoDB's last-writer-wins upsert; the worst case is re-handling events, never losing them.
        if (entity.TypeSetControl is null)
        {
            entity.TypeSetControl = existing.TypeSetControl;
        }
        else
        {
            var requested = ObserverTypeSetControlJson.Deserialize(entity.TypeSetControl);
            var stored = ObserverTypeSetControlJson.Deserialize(existing.TypeSetControl);
            if (stored is not null && requested is not null && requested.Revision.Value < stored.Revision.Value)
            {
                entity.TypeSetControl = existing.TypeSetControl;
            }
        }

        entity.Initialization ??= existing.Initialization;
    }
}
