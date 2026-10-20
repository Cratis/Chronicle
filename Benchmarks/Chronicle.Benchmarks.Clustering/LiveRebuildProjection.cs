// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Rebuilds many keys with a shared catalog join, including a dedicated live marker key.
/// </summary>
public class LiveRebuildProjection : IProjectionFor<LiveRebuildReadModel>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<LiveRebuildReadModel> builder) => builder
        .From<RebuildValueChanged>(from => from
            .Set(model => model.Id).ToEventContextProperty(context => context.EventSourceId))
        .Join<RebuildCatalogNamed>(join => join
            .On(model => model.CatalogId)
            .Set(model => model.Category).To(@event => @event.Category));
}
