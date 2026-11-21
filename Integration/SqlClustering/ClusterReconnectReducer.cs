// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.SqlClustering;

public class ClusterReconnectReducer
{
    readonly ConcurrentDictionary<(Guid Id, EventSequenceNumber Sequence), byte> _handled = new();

    public bool HasHandled(Guid id, EventSequenceNumber sequence) => _handled.ContainsKey((id, sequence));

    /// <summary>
    /// Gets the highest event sequence number handled so far, or null when nothing was handled.
    /// </summary>
    /// <returns>The highest handled <see cref="EventSequenceNumber"/>, or null.</returns>
    public EventSequenceNumber? HighestHandled() => _handled.IsEmpty ? null : new EventSequenceNumber(_handled.Keys.Max(handled => handled.Sequence.Value));

    public Task<ClusterReducedEvent?> Reduce(ClusterEventAppended @event, ClusterReducedEvent? current, EventContext context)
    {
        _handled.TryAdd((@event.Id, context.SequenceNumber), 0);
        return Task.FromResult<ClusterReducedEvent?>(new(@event.Id));
    }
}
