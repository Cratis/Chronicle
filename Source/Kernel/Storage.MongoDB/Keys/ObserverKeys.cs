// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
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
    /// <remarks>
    /// An empty <c language="csharp">eventTypes</c> means no fixed type list to key off - see
    /// <see cref="Cratis.Chronicle.Observation.Observer.SubscribeToAllEvents{TObserverSubscriber}"/> - so it must not
    /// narrow to a <c language="csharp">$in: []</c> filter, which matches no document in MongoDB. That is a
    /// different outcome from a type filter that is simply absent, so the type filter is only added when there is
    /// an actual, non-empty set of types to narrow by.
    /// </remarks>
    public IAsyncEnumerator<Key> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<Event>>
        {
            Builders<Event>.Filter.Gte(_ => _.SequenceNumber, fromEventSequenceNumber)
        };
        if (_eventTypes.Any())
        {
            filters.Add(Builders<Event>.Filter.In(_ => _.Type, _eventTypes));
        }

        var filter = Builders<Event>.Filter.And(filters);

        return new ObserverKeysAsyncEnumerator(
            ct => collection.DistinctAsync(_ => _.EventSourceId, filter, cancellationToken: ct),
            cancellationToken);
    }
}
