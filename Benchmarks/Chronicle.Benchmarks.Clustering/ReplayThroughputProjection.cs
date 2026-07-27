// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Projects <see cref="ReplayedEvent"/> into a read model keyed by the event source. Kept separate from
/// <see cref="ProjectionThroughputProjection"/> so a replay only reprocesses its own seeded corpus.
/// </summary>
public class ReplayThroughputProjection : IProjectionFor<ReplayThroughputReadModel>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<ReplayThroughputReadModel> builder) => builder
        .From<ReplayedEvent>(events => events
            .Set(model => model.Name).To(@event => @event.Name)
            .Set(model => model.Value).To(@event => @event.Value));
}
