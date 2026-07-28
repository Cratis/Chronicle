// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Keys;

/// <summary>
/// Represents an implementation of <see cref="IAsyncEnumerator{T}"/> for MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ObserverKeysAsyncEnumerator"/> class.
/// </remarks>
/// <param name="cursorFactory">Factory that opens the inner <see cref="IAsyncCursor{T}"/> when first enumerated.</param>
/// <param name="cancellationToken">The <see cref="CancellationToken"/> to open the cursor with.</param>
public class ObserverKeysAsyncEnumerator(
    Func<CancellationToken, Task<IAsyncCursor<EventSourceId>>> cursorFactory,
    CancellationToken cancellationToken) : IAsyncEnumerator<Key>
{
    Key? _current;
    Queue<Key>? _queue;
    IAsyncCursor<EventSourceId>? _cursor;

    /// <inheritdoc/>
    public Key Current => _current!;

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        _cursor?.Dispose();
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public async ValueTask<bool> MoveNextAsync()
    {
        // Open the cursor lazily so the whole-collection distinct runs asynchronously on first enumeration
        // rather than blocking a thread when the enumerator is created.
        _cursor ??= await cursorFactory(cancellationToken);

        if (_queue is null)
        {
            var result = await _cursor.MoveNextAsync(cancellationToken);
            if (!result)
            {
                _current = null;
                return false;
            }

            _queue = new(_cursor.Current.Select(_ => new Key(_.Value, ArrayIndexers.NoIndexers)));
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
