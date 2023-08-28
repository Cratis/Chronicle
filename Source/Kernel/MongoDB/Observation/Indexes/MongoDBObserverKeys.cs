// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Keys;
using Aksio.Cratis.Kernel.Observation;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Observation.Indexes;

/// <summary>
/// Represents an implementation of <see cref="IObserverKeys"/> for MongoDB.
/// </summary>
public class MongoDBObserverKeys : IObserverKeys
{
    readonly IMongoCollection<Event> _collection;

    public MongoDBObserverKeys(IMongoCollection<Event> collection)
    {
        _collection = collection;
    }

    /// <inheritdoc/>
    public IAsyncEnumerator<Key> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        var cursor = _collection.Distinct(_ => _.EventSourceId, _ => true, cancellationToken: cancellationToken);
        return new MongoDBObserverKeysAsyncEnumerator(cursor);
    }
}

/// <summary>
/// Represents an implementation of <see cref="IAsyncEnumerator{T}"/> for MongoDB.
/// </summary>
public class MongoDBObserverKeysAsyncEnumerator : IAsyncEnumerator<Key>
{
    readonly IAsyncCursor<Key> _cursor;
    readonly Key? _current;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBObserverKeysAsyncEnumerator"/> class.
    /// </summary>
    /// <param name="cursor"></param>
    public MongoDBObserverKeysAsyncEnumerator(IAsyncCursor<Key> cursor)
    {
        _cursor = cursor;
    }

    /// <inheritdoc/>
    public Key Current => _current!;

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        _cursor.Dispose();
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public async ValueTask<bool> MoveNextAsync() => await _cursor.MoveNextAsync();
}
