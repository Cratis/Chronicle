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
    public Task<TAggregateRoot> Get<TAggregateRoot>(EventSourceId id, bool autoCommit = true)
        where TAggregateRoot : IAggregateRoot
    {
        return Task.FromResult(grainFactory.GetGrain<TAggregateRoot>(id.Value));
    }
}
