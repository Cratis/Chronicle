// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Keys;
using Aksio.Cratis.Kernel.Observation;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Observation.Indexes;

/// <summary>
/// Represents an implementation of <see cref="IObserverKeyIndex"/> for MongoDB.
/// </summary>
public class MongoDBEventSourceKeyIndex : IObserverKeyIndex
{
    readonly IMongoCollection<Event> _collection;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBEventSourceKeyIndex"/> class.
    /// </summary>
    /// <param name="collection"><see cref="IMongoCollection{T}"/> for the events.</param>
    public MongoDBEventSourceKeyIndex(IMongoCollection<Event> collection)
    {
        _collection = collection;
    }

    /// <inheritdoc/>
    public Task Add(Key key) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task<IObserverKeys> GetKeysFor()
    {
        var cursor = await _collection.DistinctAsync(_ => _.EventSourceId, _ => true);
        return new MongoDBObserverKeys(cursor);
    }

    /// <inheritdoc/>
    public Task Rebuild() => throw new NotImplementedException();
}
