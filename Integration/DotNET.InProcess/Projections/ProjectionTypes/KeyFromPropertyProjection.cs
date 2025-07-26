// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.InProcess.Integration.Projections.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Models;

namespace Cratis.Chronicle.InProcess.Integration.Projections.ProjectionTypes;

public class KeyFromPropertyProjection : IProjectionFor<ReadModel>
{
    public void Define(IProjectionBuilderFor<ReadModel> builder) => builder
        .From<EventWithPropertiesForAllSupportedTypes>(_ => _
            .UsingKey(e => e.StringValue)
            .Set(m => m.LastUpdated).ToEventContextProperty(e => e.Occurred));
}
