// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Projections.for_Projection.when_reading_with_pinned_delivery;

public class and_delivery_is_compatibility : given.a_pinned_projection_grain
{
    protected override EventGenerationDelivery Delivery => EventGenerationDelivery.Compatibility;

    async Task Because() => await Fold();

    [Fact] void should_keep_the_definition_unchanged() => _engineDefinition.ShouldEqual(_definition);
    [Fact] void should_not_release_the_event() => _projection.DidNotReceive().ReleaseEvent(Arg.Any<EventType>(), Arg.Any<AppendedEvent>());
    [Fact] void should_project_the_stored_event() => _projected.Event.ShouldBeSame(_stored);
}
