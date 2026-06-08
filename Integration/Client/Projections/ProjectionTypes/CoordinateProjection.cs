// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Events;
using Cratis.Chronicle.Integration.Projections.ReadModels;

namespace Cratis.Chronicle.Integration.Projections.ProjectionTypes;

public class CoordinateProjection : IProjectionFor<CoordinateReadModel>
{
    public void Define(IProjectionBuilderFor<CoordinateReadModel> builder) => builder
        .AutoMap()
        .From<CoordinateEvent>(fb => fb
            .Set(m => m.Location).To(e => e.Location));
}
