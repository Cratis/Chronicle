// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Orleans.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootFactory"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
public class AggregateRootFactory(IGrainFactory grainFactory) : IAggregateRootFactory
{
    /// <inheritdoc/>
    public Task<TAggregateRoot> Get<TAggregateRoot>(EventSourceId id, EventStreamId? streamId = default, EventSourceType? eventSource = default)
        where TAggregateRoot : IAggregateRoot
    {
        var key = new AggregateRootKey(eventSource ?? EventSourceType.Default, id, streamId ?? EventStreamId.Default);
        return Task.FromResult(grainFactory.GetGrain<TAggregateRoot>((string)key));
    }
}
