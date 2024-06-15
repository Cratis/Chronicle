// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Events;
using Cratis.Properties;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Keys;

/// <summary>
/// Represents an implementation of <see cref="IAsyncEnumerator{T}"/> for MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ObserverKeysAsyncEnumerator"/> class.
/// </remarks>
/// <param name="cursor">The inner <see cref="IAsyncCursor{T}"/>.</param>
public class ObserverKeysAsyncEnumerator(IAsyncCursor<EventSourceId> cursor) : IAsyncEnumerator<Key>
{
    Key? _current;
    Queue<Key>? _queue;

    /// <inheritdoc/>
    public Key Current => _current!;

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        cursor.Dispose();
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public async ValueTask<bool> MoveNextAsync()
    {
        if (_queue is null)
        {
            var result = await cursor.MoveNextAsync();
            if (!result)
            {
                _current = null;
                return false;
            }

            _queue = new Queue<Key>(cursor.Current.Select(_ => new Key(_.Value, ArrayIndexers.NoIndexers)));
        }

        if (_queue.Count == 0)
        {
            _current = null;
            return false;
        }

        _current = _queue.Dequeue();
        if (_queue.Count == 0)
        {
            _queue = null;
        }

        return true;
    }
}
