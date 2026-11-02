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
    /// <inheritdoc/>
    public Task Increment(ObserverId observerId, Key partition, IReadOnlyDictionary<EventTypeId, EventCount> countsPerEventType) =>
        Increment(observerId, [new(partition, countsPerEventType)]);

    /// <inheritdoc/>
    public async Task Increment(ObserverId observerId, IReadOnlyCollection<KeyValuePair<Key, IReadOnlyDictionary<EventTypeId, EventCount>>> countsPerPartition)
    {
        var cells = countsPerPartition.SelectMany(partition => partition.Value.Select(count => new
        {
            Partition = partition.Key.ToString(),
            EventType = count.Key.Value,
            Count = (long)count.Value.Value
        })).GroupBy(cell => (cell.Partition, cell.EventType))
            .OrderBy(group => group.Key.Partition, StringComparer.Ordinal)
            .ThenBy(group => group.Key.EventType, StringComparer.Ordinal)
            .Select(group => new ObserverHandledCountEntry
            {
                Id = Guid.NewGuid(),
                ObserverId = observerId.Value,
                Partition = group.Key.Partition,
                EventTypeId = group.Key.EventType,
                Count = group.Sum(cell => cell.Count)
            }).ToArray();
        if (cells.Length == 0) return;

        await using var scope = await database.Namespace(eventStore, @namespace);
        await ObserverHandledCountsSql.Increment(scope.DbContext, cells);
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
}
