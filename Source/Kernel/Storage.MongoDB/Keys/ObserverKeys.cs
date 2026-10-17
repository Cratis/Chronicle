// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
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
/// <param name="scope">The <see cref="ObserverReaderScope"/> the index is for.</param>
public class ObserverKeys(
    IMongoCollection<Event> collection,
    EventSequenceNumber fromEventSequenceNumber,
    ObserverReaderScope scope) : IObserverKeys
{
    readonly IEnumerable<EventTypeId> _eventTypes = scope.EventTypes.Select(_ => _.Id).ToArray();

    /// <inheritdoc/>
    /// <remarks>
    /// A wildcard scope has no fixed type list to key off - see
    /// <see cref="Cratis.Chronicle.Observation.Observer.SubscribeToAllEvents{TObserverSubscriber}"/> - so it adds no
    /// type filter. A <c language="csharp">$in: []</c> filter would match no document in MongoDB, which is a
    /// different outcome from a type filter that is simply absent. An explicit scope narrows by its types.
    /// </remarks>
    public IAsyncEnumerator<Key> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<Event>>
        {
            Builders<Event>.Filter.Gte(_ => _.SequenceNumber, fromEventSequenceNumber)
        };
        if (!scope.IsWildcard)
        {
            filters.Add(Builders<Event>.Filter.In(_ => _.Type, _eventTypes));
        }

        var filter = Builders<Event>.Filter.And(filters);

        return new ObserverKeysAsyncEnumerator(
            ct => collection.DistinctAsync(_ => _.EventSourceId, filter, cancellationToken: ct),
            cancellationToken);
    }
}
