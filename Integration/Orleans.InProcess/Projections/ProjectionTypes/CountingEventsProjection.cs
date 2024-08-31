// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Events;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Models;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.Projections.ProjectionTypes;

public class CountingEventsProjection : IProjectionFor<Model>
{
    public void Define(IProjectionBuilderFor<Model> builder) => builder
        .From<EventWithPropertiesForAllSupportedTypes>(_ => _
            .Count(m => m.IntValue));
}
