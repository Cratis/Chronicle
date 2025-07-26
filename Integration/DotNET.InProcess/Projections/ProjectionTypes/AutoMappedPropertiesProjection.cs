// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.InProcess.Integration.Projections.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Models;

namespace Cratis.Chronicle.InProcess.Integration.Projections.ProjectionTypes;

public class AutoMappedPropertiesProjection : IProjectionFor<ReadModel>
{
    public void Define(IProjectionBuilderFor<ReadModel> builder) => builder
        .AutoMap()
        .From<EventWithPropertiesForAllSupportedTypes>();
}
