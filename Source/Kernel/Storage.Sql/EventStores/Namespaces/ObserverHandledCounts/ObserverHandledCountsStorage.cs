// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverHandledCounts;

/// <summary>
/// Represents an implementation of <see cref="IObserverHandledCountsStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The <see cref="EventStoreName"/> the storage is for.</param>
/// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the storage is for.</param>
/// <param name="database">The <see cref="IDatabase"/> to use.</param>
public class ObserverHandledCountsStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, IDatabase database) : IObserverHandledCountsStorage
{
    const int MaxAttempts = 5;

    /// <inheritdoc/>
    public async Task Increment(ObserverId observerId, Key partition, IReadOnlyDictionary<EventTypeId, EventCount> countsPerEventType)
    {
        foreach (var (eventTypeId, count) in countsPerEventType)
        {
            await Increment(observerId.Value, partition.ToString(), eventTypeId.Value, (long)count.Value);
        }
    }

    /// <inheritdoc/>
    public async Task Increment(ObserverId observerId, IReadOnlyCollection<KeyValuePair<Key, IReadOnlyDictionary<EventTypeId, EventCount>>> countsPerPartition)
    {
        foreach (var (partition, counts) in countsPerPartition)
        {
            await Increment(observerId, partition, counts);
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<EventTypeId, EventCount>> GetFor(ObserverId observerId, Key partition)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var observer = observerId.Value;
        var partitionValue = partition.ToString();
        var rows = await scope.DbContext.ObserverHandledCounts
            .AsNoTracking()
            .Where(_ => _.ObserverId == observer && _.Partition == partitionValue)
            .ToListAsync();

        if (rows.Count == 0)
        {
            return ImmutableDictionary<EventTypeId, EventCount>.Empty;
        }

        return rows.ToDictionary(_ => (EventTypeId)_.EventTypeId, _ => new EventCount((ulong)_.Count));
    }

    /// <inheritdoc/>
    public async Task SetForRecovery(ObserverId observerId, Key partition, IReadOnlyDictionary<EventTypeId, EventCount> countsPerEventType)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var observer = observerId.Value;
        var partitionValue = partition.ToString();
        var rows = await scope.DbContext.ObserverHandledCounts.AsTracking()
            .Where(_ => _.ObserverId == observer && _.Partition == partitionValue).ToListAsync();
        foreach (var (eventTypeId, count) in countsPerEventType)
        {
            var row = rows.SingleOrDefault(_ => _.EventTypeId == eventTypeId.Value);
            if (row is null)
            {
                row = new ObserverHandledCountEntry { ObserverId = observer, Partition = partitionValue, EventTypeId = eventTypeId.Value };
                scope.DbContext.ObserverHandledCounts.Add(row);
            }
            row.Count = (long)count.Value;
        }
        await scope.DbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task RemoveFor(ObserverId observerId, Key partition)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var observer = observerId.Value;
        var partitionValue = partition.ToString();
        await scope.DbContext.ObserverHandledCounts
            .Where(_ => _.ObserverId == observer && _.Partition == partitionValue)
            .ExecuteDeleteAsync();
    }

    /// <inheritdoc/>
    public async Task RemoveAllFor(ObserverId observerId)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var observer = observerId.Value;
        await scope.DbContext.ObserverHandledCounts
            .Where(_ => _.ObserverId == observer)
            .ExecuteDeleteAsync();
    }

    async Task Increment(string observer, string partition, string eventType, long addend)
    {
        for (var attempt = 1; ; attempt++)
        {
            await using var scope = await database.Namespace(eventStore, @namespace);

            // Atomic server-side increment; no read-modify-write, so concurrent writers never lose counts.
            var updated = await scope.DbContext.ObserverHandledCounts
                .Where(_ => _.ObserverId == observer && _.Partition == partition && _.EventTypeId == eventType)
                .ExecuteUpdateAsync(_ => _.SetProperty(row => row.Count, row => row.Count + addend));
            if (updated > 0)
            {
                return;
            }

            try
            {
                scope.DbContext.ObserverHandledCounts.Add(new ObserverHandledCountEntry
                {
                    ObserverId = observer,
                    Partition = partition,
                    EventTypeId = eventType,
                    Count = addend
                });
                await scope.DbContext.SaveChangesAsync();
                return;
            }
            catch (DbUpdateException) when (attempt < MaxAttempts)
            {
                // A concurrent writer inserted the row first; loop around and increment it instead.
            }
        }
    }
}
