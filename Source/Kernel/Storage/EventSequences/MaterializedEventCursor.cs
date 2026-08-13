// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Represents an <see cref="IEventCursor"/> over a set of events that has already been read into memory.
/// </summary>
/// <param name="events">The events the cursor yields, in the order they should be observed.</param>
/// <remarks>
/// Use this when the result set is already bounded - a single page, for instance - and the order it was
/// read in must be preserved. Cursors that re-query the backing store are free to impose their own order.
/// </remarks>
public sealed class MaterializedEventCursor(IReadOnlyList<AppendedEvent> events) : IEventCursor
{
    bool _moved;

    /// <inheritdoc/>
    public IEnumerable<AppendedEvent> Current { get; private set; } = [];

    /// <inheritdoc/>
    public Task<bool> MoveNext()
    {
        if (_moved)
        {
            Current = [];
            return Task.FromResult(false);
        }

        _moved = true;
        Current = events;

        return Task.FromResult(events.Count > 0);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
