// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.for_Projection.when_reading_with_pinned_delivery;

public class and_the_stored_generation_is_the_pin : given.a_pinned_projection_grain
{
    void Establish() => _stored = AppendedEvent.EmptyWithEventType(_pin);

    async Task Because() => await Fold();

    [Fact] void should_not_release_the_event() => _projection.DidNotReceive().ReleaseEvent(Arg.Any<EventType>(), Arg.Any<AppendedEvent>());
    [Fact] void should_project_the_stored_event() => _projected.Event.ShouldBeSame(_stored);
}
