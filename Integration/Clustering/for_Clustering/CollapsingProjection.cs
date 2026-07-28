// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Integration.Clustering.for_Clustering;

/// <summary>
/// A projection whose key comes from event content, so every event source in a group collapses onto the same read
/// model document, and whose only property is accumulated with a read-modify-write.
/// </summary>
public class CollapsingProjection : IProjectionFor<CollapsingProjectionReadModel>
{
    public ProjectionId Identifier => "collapsing-projection";

    public void Define(IProjectionBuilderFor<CollapsingProjectionReadModel> builder) => builder
        .From<CollapsedEvent>(events => events
            .UsingKey(@event => @event.Group)
            .Increment(model => model.Count));
}
