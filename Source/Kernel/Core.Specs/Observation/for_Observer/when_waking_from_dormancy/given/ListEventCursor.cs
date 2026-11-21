// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Observation.for_Observer.when_waking_from_dormancy.given;

/// <summary>
/// A cursor over events held in memory, delivered as one batch.
/// </summary>
/// <param name="events">The events to deliver.</param>
public sealed class ListEventCursor(IEnumerable<AppendedEvent> events) : IEventCursor
{
    bool _delivered;

    /// <inheritdoc/>
    public IEnumerable<AppendedEvent> Current { get; private set; } = [];

    /// <inheritdoc/>
    public Task<bool> MoveNext()
    {
        if (_delivered)
        {
            Current = [];
            return Task.FromResult(false);
        }

        _delivered = true;
        Current = events;
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
