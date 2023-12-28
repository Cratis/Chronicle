// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Keys;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Keys;

/// <summary>
/// Represents an implementation of <see cref="IObserverKeys"/> for MongoDB.
/// </summary>
public class MongoDBObserverKeys : IObserverKeys
{
    readonly IMongoCollection<Event> _collection;
    readonly EventSequenceNumber _fromEventSequenceNumber;
    readonly IEnumerable<EventTypeId> _eventTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBObserverKeys"/> class.
    /// </summary>
    /// <param name="collection">The <see cref="IMongoCollection{T}"/> that holds the keys.</param>
    /// <param name="fromEventSequenceNumber">The <see cref="EventSequenceNumber"/> we want to get keys starting from.</param>
    /// <param name="eventTypes">Collection of <see cref="EventType"/> the index is for.</param>
    public MongoDBObserverKeys(
        IMongoCollection<Event> collection,
        EventSequenceNumber fromEventSequenceNumber,
        IEnumerable<EventType> eventTypes)
    {
        _collection = collection;
        _fromEventSequenceNumber = fromEventSequenceNumber;
        _eventTypes = eventTypes.Select(_ => _.Id).ToArray();
    }

    /// <inheritdoc/>
    public IAsyncEnumerator<Key> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        var filter = Builders<Event>.Filter.And(
            Builders<Event>.Filter.Gt(_ => _.SequenceNumber, _fromEventSequenceNumber),
            Builders<Event>.Filter.In(_ => _.Type, _eventTypes));

        var cursor = _collection.DistinctAsync(_ => _.EventSourceId, filter, cancellationToken: cancellationToken)
                                .ConfigureAwait(false)
                                .GetAwaiter()
                                .GetResult();
        return new MongoDBObserverKeysAsyncEnumerator(cursor);
    }
}
