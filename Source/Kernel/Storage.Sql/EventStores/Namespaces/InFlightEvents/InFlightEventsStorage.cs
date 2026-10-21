// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.InFlightEvents;

/// <summary>
/// Represents an implementation of <see cref="IInFlightEventsStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The <see cref="EventStoreName"/> the storage is for.</param>
/// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the storage is for.</param>
/// <param name="database">The <see cref="IDatabase"/> to use.</param>
public class InFlightEventsStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, IDatabase database) : IInFlightEventsStorage
{
    /// <inheritdoc/>
    public async Task Add(ObserverId observerId, Key partition, EventSequenceNumber eventSequenceNumber)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        scope.DbContext.InFlightEvents.Add(new InFlightEventEntry
        {
            Id = InFlightEventId.New().Value,
            ObserverId = observerId.Value,
            Partition = partition.ToString(),
            EventSequenceNumber = eventSequenceNumber.Value
        });
        await scope.DbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task Remove(ObserverId observerId, Key partition, EventSequenceNumber eventSequenceNumber)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var observer = observerId.Value;
        var partitionValue = partition.ToString();
        decimal sequenceNumber = eventSequenceNumber.Value;
        await scope.DbContext.InFlightEvents
            .Where(_ => _.ObserverId == observer && _.Partition == partitionValue && _.EventSequenceNumber == sequenceNumber)
            .ExecuteDeleteAsync();
    }

    /// <inheritdoc/>
    public async Task RemoveUpTo(ObserverId observerId, Key partition, EventSequenceNumber upToInclusive)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var observer = observerId.Value;
        var partitionValue = partition.ToString();
        decimal upTo = upToInclusive.Value;
        await scope.DbContext.InFlightEvents
            .Where(_ => _.ObserverId == observer && _.Partition == partitionValue && _.EventSequenceNumber <= upTo)
            .ExecuteDeleteAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<InFlightEvent>> GetFor(ObserverId observerId)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var observer = observerId.Value;
        var entries = await scope.DbContext.InFlightEvents
            .AsNoTracking()
            .Where(_ => _.ObserverId == observer)
            .ToListAsync();

        return entries.Select(_ => new InFlightEvent
        {
            Id = _.Id,
            ObserverId = _.ObserverId,
            Partition = new Key(_.Partition, ArrayIndexers.NoIndexers),
            EventSequenceNumber = (ulong)_.EventSequenceNumber
        }).ToArray();
    }
}
