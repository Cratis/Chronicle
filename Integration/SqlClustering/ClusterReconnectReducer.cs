// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.SqlClustering;

public class ClusterReconnectReducer
{
    readonly ConcurrentDictionary<(Guid Id, EventSequenceNumber Sequence), byte> _handled = new();

    public bool HasHandled(Guid id, EventSequenceNumber sequence) => _handled.ContainsKey((id, sequence));

    public Task<ClusterReducedEvent?> Reduce(ClusterEventAppended @event, ClusterReducedEvent? current, EventContext context)
    {
        _handled.TryAdd((@event.Id, context.SequenceNumber), 0);
        return Task.FromResult<ClusterReducedEvent?>(new(@event.Id));
    }
}
