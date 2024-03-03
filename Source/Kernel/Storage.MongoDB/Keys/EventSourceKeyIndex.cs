// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Keys;
using Aksio.Cratis.Kernel.Storage.Keys;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.Storage.MongoDB.Keys;

/// <summary>
/// Represents an implementation of <see cref="IObserverKeyIndex"/> for MongoDB.
/// </summary>
public class EventSourceKeyIndex : IObserverKeyIndex
{
    readonly IMongoCollection<Event> _collection;
    readonly IEnumerable<EventType> _eventTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSourceKeyIndex"/> class.
    /// </summary>
    /// <param name="collection"><see cref="IMongoCollection{T}"/> for the events.</param>
    /// <param name="eventTypes">Collection of <see cref="EventType"/> the index is for.</param>
    public EventSourceKeyIndex(IMongoCollection<Event> collection, IEnumerable<EventType> eventTypes)
    {
        _collection = collection;
        _eventTypes = eventTypes;
    }

    /// <inheritdoc/>
    public Task Add(Key key) => Task.CompletedTask;

    /// <inheritdoc/>
    public IObserverKeys GetKeys(EventSequenceNumber fromEventSequenceNumber) =>
       new ObserverKeys(_collection, fromEventSequenceNumber, _eventTypes);

    /// <inheritdoc/>
    public Task Rebuild() => Task.CompletedTask;
}
