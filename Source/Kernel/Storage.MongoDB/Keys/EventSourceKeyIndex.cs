// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Keys;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Keys;

/// <summary>
/// Represents an implementation of <see cref="IObserverKeyIndex"/> for MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventSourceKeyIndex"/> class.
/// </remarks>
/// <param name="collection"><see cref="IMongoCollection{T}"/> for the events.</param>
/// <param name="scope">The <see cref="ObserverReaderScope"/> the index is for.</param>
public class EventSourceKeyIndex(IMongoCollection<Event> collection, ObserverReaderScope scope) : IObserverKeyIndex
{
    /// <inheritdoc/>
    public Task Add(Key key) => Task.CompletedTask;

    /// <inheritdoc/>
    public IObserverKeys GetKeys(EventSequenceNumber fromEventSequenceNumber) =>
       new ObserverKeys(collection, fromEventSequenceNumber, scope);

    /// <inheritdoc/>
    public Task Rebuild() => Task.CompletedTask;
}
