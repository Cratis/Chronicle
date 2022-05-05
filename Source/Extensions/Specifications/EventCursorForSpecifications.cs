// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store;

namespace Aksio.Cratis.Specifications;

/// <summary>
/// Represents an <see cref="IEventCursor"/> for specifications.
/// </summary>
public class EventCursorForSpecifications : IEventCursor
{
    bool _moveNext = true;

    /// <inheritdoc/>
    public IEnumerable<AppendedEvent> Current { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventCursorForSpecifications"/> class.
    /// </summary>
    /// <param name="all">All events.</param>
    public EventCursorForSpecifications(IEnumerable<AppendedEvent> all) => Current = all;

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    /// <inheritdoc/>
    public Task<bool> MoveNext()
    {
        var move = _moveNext;
        _moveNext = false;
        return Task.FromResult(move);
    }
}
