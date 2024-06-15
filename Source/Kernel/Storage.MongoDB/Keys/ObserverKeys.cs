// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Storage.Keys;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Keys;

/// <summary>
/// Represents an implementation of <see cref="IObserverKeys"/> for MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ObserverKeys"/> class.
/// </remarks>
/// <param name="collection">The <see cref="IMongoCollection{T}"/> that holds the keys.</param>
/// <param name="fromEventSequenceNumber">The <see cref="EventSequenceNumber"/> we want to get keys starting from.</param>
/// <param name="eventTypes">Collection of <see cref="EventType"/> the index is for.</param>
public class ObserverKeys(
    IMongoCollection<Event> collection,
    EventSequenceNumber fromEventSequenceNumber,
    IEnumerable<EventType> eventTypes) : IObserverKeys
{
    readonly IEnumerable<EventTypeId> _eventTypes = eventTypes.Select(_ => _.Id).ToArray();

    /// <inheritdoc/>
    public IAsyncEnumerator<Key> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        var filter = Builders<Event>.Filter.And(
            Builders<Event>.Filter.Gt(_ => _.SequenceNumber, fromEventSequenceNumber),
            Builders<Event>.Filter.In(_ => _.Type, _eventTypes));

        var cursor = collection.DistinctAsync(_ => _.EventSourceId, filter, cancellationToken: cancellationToken)
                                .ConfigureAwait(false)
                                .GetAwaiter()
                                .GetResult();
        return new ObserverKeysAsyncEnumerator(cursor);
    }
}
