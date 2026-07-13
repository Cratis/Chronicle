// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Integration.Clustering.for_Clustering;

public class ClusteredProjection : IProjectionFor<ClusteredProjectionReadModel>
{
    public ProjectionId Identifier => "clustered-projection";

    public void Define(IProjectionBuilderFor<ClusteredProjectionReadModel> builder) => builder
        .From<ClusteredEvent>(events => events
            .Set(model => model.Number)
            .To(@event => @event.Number));
}
