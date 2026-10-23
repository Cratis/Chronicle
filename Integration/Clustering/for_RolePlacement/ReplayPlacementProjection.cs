// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Integration.Clustering.for_RolePlacement;

public class ReplayPlacementProjection : IProjectionFor<ReplayPlacementModel>
{
    public ProjectionId Identifier => ReplayPlacementSignal.ProjectionId;

    public void Define(IProjectionBuilderFor<ReplayPlacementModel> builder) => builder
        .From<ReplayPlaced>(events => events.Set(model => model.Number).To(@event => @event.Number));
}
