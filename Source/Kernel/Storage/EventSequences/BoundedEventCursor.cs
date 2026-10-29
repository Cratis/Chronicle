// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Represents an <see cref="IEventCursor"/> that never presents more than a given number of events at a time,
/// splitting each batch of the cursor it wraps.
/// </summary>
/// <remarks>
/// How many events a storage cursor batch holds depends on the storage, not on the consumer: a MongoDB cursor
/// batch can carry thousands. Wrapping it bounds the work behind one step of the walk, without changing the order
/// or the set of events.
/// </remarks>
/// <param name="inner">The <see cref="IEventCursor"/> to read from.</param>
/// <param name="maximumEventsPerBatch">The most events presented at a time; zero or less leaves the batches as they are.</param>
public sealed class BoundedEventCursor(IEventCursor inner, int maximumEventsPerBatch) : IEventCursor
{
    AppendedEvent[] _pending = [];
    int _offset;

    /// <inheritdoc/>
    public IEnumerable<AppendedEvent> Current { get; private set; } = [];

    /// <inheritdoc/>
    public async Task<bool> MoveNext()
    {
        if (maximumEventsPerBatch <= 0)
        {
            var moved = await inner.MoveNext();
            Current = moved ? inner.Current : [];
            return moved;
        }

        while (_offset >= _pending.Length)
        {
            if (!await inner.MoveNext())
            {
                Current = [];
                return false;
            }

            _pending = inner.Current.ToArray();
            _offset = 0;
        }

        var count = Math.Min(maximumEventsPerBatch, _pending.Length - _offset);
        Current = new ArraySegment<AppendedEvent>(_pending, _offset, count).ToArray();
        _offset += count;
        return true;
    }

    /// <inheritdoc/>
    public void Dispose() => inner.Dispose();
}
