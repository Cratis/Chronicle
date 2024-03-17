// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Cratis.Kernel.Storage.EventSequences;

namespace Cratis.Specifications;

/// <summary>
/// Represents an <see cref="IEventCursor"/> for specifications.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventCursorForSpecifications"/> class.
/// </remarks>
/// <param name="all">All events.</param>
public class EventCursorForSpecifications(IEnumerable<AppendedEvent> all) : IEventCursor
{
    bool _moveNext = true;

    /// <inheritdoc/>
    public IEnumerable<AppendedEvent> Current { get; } = all;

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
