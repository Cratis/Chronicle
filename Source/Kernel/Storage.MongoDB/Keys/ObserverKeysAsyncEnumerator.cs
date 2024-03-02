// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Keys;
using Aksio.Cratis.Properties;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.Storage.MongoDB.Keys;

/// <summary>
/// Represents an implementation of <see cref="IAsyncEnumerator{T}"/> for MongoDB.
/// </summary>
public class ObserverKeysAsyncEnumerator : IAsyncEnumerator<Key>
{
    readonly IAsyncCursor<EventSourceId> _cursor;
    Key? _current;
    Queue<Key>? _queue;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverKeysAsyncEnumerator"/> class.
    /// </summary>
    /// <param name="cursor">The inner <see cref="IAsyncCursor{T}"/>.</param>
    public ObserverKeysAsyncEnumerator(IAsyncCursor<EventSourceId> cursor)
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
        if (_queue is null)
        {
            var result = await _cursor.MoveNextAsync();
            if (!result)
            {
                _current = null;
                return false;
            }

            _queue = new Queue<Key>(_cursor.Current.Select(_ => new Key(_.Value, ArrayIndexers.NoIndexers)));
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
