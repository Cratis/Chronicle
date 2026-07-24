// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation;

/// <summary>
/// Represents an implementation of <see cref="IInFlightEventsStorage"/> for MongoDB.
/// </summary>
/// <param name="database">The <see cref="IEventStoreNamespaceDatabase"/> to use for accessing the collection.</param>
public class InFlightEventsStorage(IEventStoreNamespaceDatabase database) : IInFlightEventsStorage
{
    readonly IMongoCollection<InFlightEvent> _collection = database.GetCollection<InFlightEvent>(WellKnownCollectionNames.InFlightEvents);
    readonly ConcurrentDictionary<string, byte> _ensuredIndexes = new();

    /// <inheritdoc/>
    public async Task Add(ObserverId observerId, Key partition, EventSequenceNumber eventSequenceNumber)
    {
        await EnsureIndexes().ConfigureAwait(false);
        var entry = new InFlightEvent
        {
            Id = InFlightEventId.New(),
            ObserverId = observerId,
            Partition = partition,
            EventSequenceNumber = eventSequenceNumber
        };

        await _collection.InsertOneAsync(entry).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task Remove(ObserverId observerId, Key partition, EventSequenceNumber eventSequenceNumber)
    {
        await EnsureIndexes().ConfigureAwait(false);
        await _collection.DeleteManyAsync(
            entry =>
                entry.ObserverId == observerId &&
                entry.Partition == partition &&
                entry.EventSequenceNumber == eventSequenceNumber).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RemoveUpTo(ObserverId observerId, Key partition, EventSequenceNumber upToInclusive)
    {
        await EnsureIndexes().ConfigureAwait(false);
        await _collection.DeleteManyAsync(
            entry =>
                entry.ObserverId == observerId &&
                entry.Partition == partition &&
                entry.EventSequenceNumber <= upToInclusive).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<InFlightEvent>> GetFor(ObserverId observerId)
    {
        await EnsureIndexes().ConfigureAwait(false);
        using var cursor = await _collection.FindAsync(entry => entry.ObserverId == observerId).ConfigureAwait(false);
        return await cursor.ToListAsync().ConfigureAwait(false);
    }

    Task EnsureIndexes() =>
        _collection.EnsureIndexesOnceAsync(
            _ensuredIndexes,
            new CreateIndexModel<InFlightEvent>(
                Builders<InFlightEvent>.IndexKeys
                    .Ascending(_ => _.ObserverId)
                    .Ascending(_ => _.Partition)
                    .Ascending(_ => _.EventSequenceNumber),
                new CreateIndexOptions { Name = "observerId_partition_eventSequenceNumber", Background = true }));
}
