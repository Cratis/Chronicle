// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Projects <see cref="ProjectedEvent"/> into a read model keyed by the event source.
/// </summary>
public class ProjectionThroughputProjection : IProjectionFor<ProjectionThroughputReadModel>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<ProjectionThroughputReadModel> builder) => builder
        .From<ProjectedEvent>(events => events
            .Set(model => model.Name).To(@event => @event.Name)
            .Set(model => model.Value).To(@event => @event.Value));
}
