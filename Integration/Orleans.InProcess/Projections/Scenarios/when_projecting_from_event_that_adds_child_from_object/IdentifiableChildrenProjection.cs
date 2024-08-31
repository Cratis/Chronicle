// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Events;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Models;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.when_projecting_from_event_that_adds_child_from_object;

public class IdentifiableChildrenProjection : IProjectionFor<ModelWithChildren>
{
    public void Define(IProjectionBuilderFor<ModelWithChildren> builder) => builder
        .From<EventWithChildObject>(_ => _
            .AddChild(m => m.Children, _ => _
                .IdentifiedBy(m => m.StringValue)
                .UsingKey(e => e.Child.StringValue)
                .FromObject(e => e.Child)));
}
