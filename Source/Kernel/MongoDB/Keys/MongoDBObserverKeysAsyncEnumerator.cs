// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Keys;
using Aksio.Cratis.Properties;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Keys;

/// <summary>
/// Represents an implementation of <see cref="IAsyncEnumerator{T}"/> for MongoDB.
/// </summary>
public class MongoDBObserverKeysAsyncEnumerator : IAsyncEnumerator<Key>
{
    readonly IAsyncCursor<EventSourceId> _cursor;
    Key? _current;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBObserverKeysAsyncEnumerator"/> class.
    /// </summary>
    /// <param name="cursor">The inner <see cref="IAsyncCursor{T}"/>.</param>
    public MongoDBObserverKeysAsyncEnumerator(IAsyncCursor<EventSourceId> cursor)
    {
        _cursor = cursor;
        _current = default;
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
    public async ValueTask<bool> MoveNextAsync()
    {
        var result = await _cursor.MoveNextAsync();
        if (!result)
        {
            _current = null;
        }
        _current = new(_cursor.Current.First(), ArrayIndexers.NoIndexers);
        return true;
    }
}
