// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.InProcess.Integration.Projections.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Models;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_removing;

public class ProjectionWithRootRemove : IProjectionFor<Model>
{
    public void Define(IProjectionBuilderFor<Model> builder) => builder
        .AutoMap()
        .From<EventWithPropertiesForAllSupportedTypes>()
        .RemovedWith<RemoveRoot>();
}
