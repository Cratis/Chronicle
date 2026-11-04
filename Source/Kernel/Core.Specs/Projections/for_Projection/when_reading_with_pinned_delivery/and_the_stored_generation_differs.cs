// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Projections.for_Projection.when_reading_with_pinned_delivery;

public class and_the_stored_generation_differs : given.a_pinned_projection_grain
{
    async Task Because() => await Fold();

    [Fact] void should_build_the_engine_with_pinned_delivery() => _engineDefinition.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
    [Fact] void should_release_the_event_to_its_pin() => _projection.Received(1).ReleaseEvent(_pin, _stored);
    [Fact] void should_resolve_the_key_from_the_released_event() => _resolved.ShouldBeSame(_released);
    [Fact] void should_project_the_released_event() => _projected.Event.ShouldBeSame(_released);
}
