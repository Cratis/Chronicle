// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Events;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Models;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.Projections.ProjectionTypes;

public class FromEveryProjection : IProjectionFor<Model>
{
    public void Define(IProjectionBuilderFor<Model> builder) => builder
        .FromEvery(_ => _
            .Set(m => m.LastUpdated).ToEventContextProperty(c => c.Occurred))
        .From<EmptyEvent>(_ => { })
        .From<EventWithPropertiesForAllSupportedTypes>(_ => { });
}
