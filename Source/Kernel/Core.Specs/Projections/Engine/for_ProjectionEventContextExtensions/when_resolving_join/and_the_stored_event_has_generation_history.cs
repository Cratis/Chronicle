// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionEventContextExtensions.when_resolving_join;

public class and_the_stored_event_has_generation_history : given.a_join_of_a_stored_event_with_pii
{
    void Establish()
    {
        _history[2] = "{\"name\":\"protected\"}";
        _projectionSchema = _storedSchema;
    }

    void Because() => ResolveJoinOfEventStoredAt(second_generation, first_generation);

    [Fact] void should_release_history_with_the_known_stored_schema() => _eventCompliance.Received(1).ReleaseHistory(_releasedEvent, Arg.Is<IDictionary<EventType, EventTypeSchema>>(schemas => schemas.Count == 1 && schemas[second_generation].Schema == _storedSchema));
    [Fact] void should_project_the_released_event() => _results.Single().Event.ShouldEqual(_releasedEvent);
}
