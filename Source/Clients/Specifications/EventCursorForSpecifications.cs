// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Cratis.Kernel.Storage.EventSequences;

namespace Cratis.Specifications;

/// <summary>
/// Represents an <see cref="IEventCursor"/> for specifications.
/// </summary>
public class EventCursorForSpecifications : IEventCursor
{
    bool _moveNext = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventCursorForSpecifications"/> class.
    /// </summary>
    /// <param name="all">All events.</param>
    public EventCursorForSpecifications(IEnumerable<AppendedEvent> all) => Current = all;

    /// <inheritdoc/>
    public IEnumerable<AppendedEvent> Current { get; }

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
