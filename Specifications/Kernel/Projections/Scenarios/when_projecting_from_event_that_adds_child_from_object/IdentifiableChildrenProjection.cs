// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Projections.Scenarios.when_projecting_from_event_that_adds_child_from_object;

public class IdentifiableChildrenProjection : IProjectionFor<ModelWithChildren>
{
    public ProjectionId Identifier => "51209405-6616-4e89-889b-e51d7b8b0649";

    public void Define(IProjectionBuilderFor<ModelWithChildren> builder) => builder
        .From<EventWithChildObject>(_ => _
            .AddChild(m => m.Children, _ => _
                .IdentifiedBy(m => m.StringValue)
                .UsingKey(e => e.Child.StringValue)
                .FromObject(e => e.Child)));
}
