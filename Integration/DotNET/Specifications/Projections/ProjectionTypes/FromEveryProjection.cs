// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Specifications.Projections.Events;
using Cratis.Chronicle.Integration.Specifications.Projections.ReadModels;

namespace Cratis.Chronicle.Integration.Specifications.Projections.ProjectionTypes;

public class FromEveryProjection : IProjectionFor<ReadModel>
{
    public void Define(IProjectionBuilderFor<ReadModel> builder) => builder
        .FromEvery(_ => _
            .Set(m => m.LastUpdated).ToEventContextProperty(c => c.Occurred))
        .From<EmptyEvent>(_ => { })
        .From<EventWithPropertiesForAllSupportedTypes>(_ => { });
}
