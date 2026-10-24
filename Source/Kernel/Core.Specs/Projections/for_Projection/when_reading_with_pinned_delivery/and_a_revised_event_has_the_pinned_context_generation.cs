// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.for_Projection.when_reading_with_pinned_delivery;

public class and_a_revised_event_has_the_pinned_context_generation : given.a_pinned_projection_grain
{
    void Establish() => _stored = AppendedEvent.EmptyWithEventType(_pin) with
    {
        RevisedGeneration = new EventTypeGeneration(2),
        GenerationalContent = new Dictionary<int, string>
        {
            [1] = "{\"name\":\"pre-revision\"}",
            [2] = "{\"fullName\":\"revised\"}"
        }
    };

    async Task Because() => await Fold();

    [Fact] void should_release_the_revision_even_when_the_context_matches_the_pin() => _projection.Received(1).ReleaseEvent(_pin, _stored);
    [Fact] void should_resolve_the_key_from_the_released_revision() => _resolved.ShouldBeSame(_released);
    [Fact] void should_project_the_released_revision() => _projected.Event.ShouldBeSame(_released);
}
