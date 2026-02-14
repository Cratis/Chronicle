// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Specifications.Projections.Events;
using Cratis.Chronicle.Integration.Specifications.Projections.ReadModels;

namespace Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.when_projecting_from_event_that_adds_child_from_object;

public class UnidentifiableChildrenProjection : IProjectionFor<ReadModelWithChildren>
{
    public void Define(IProjectionBuilderFor<ReadModelWithChildren> builder) => builder
        .From<EventWithChildObject>(_ => _
            .AddChild(m => m.Children, _ => _
                .FromObject(e => e.Child)));
}
