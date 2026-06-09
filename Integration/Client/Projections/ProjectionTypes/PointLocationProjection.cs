// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Events;
using Cratis.Chronicle.Integration.Projections.ReadModels;

namespace Cratis.Chronicle.Integration.Projections.ProjectionTypes;

public class PointLocationProjection : IProjectionFor<PointLocationReadModel>
{
    public void Define(IProjectionBuilderFor<PointLocationReadModel> builder) => builder
        .AutoMap()
        .From<PointLocationEvent>(fb => fb
            .Set(m => m.Location).To(e => e.Location));
}
