// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionEventContextExtensions.when_resolving_join;

public class and_the_stored_event_is_of_an_earlier_generation_than_the_projection : given.a_join_of_a_stored_event_with_pii
{
    void Establish() => _projectionSchema = new JsonSchema();

    void Because() => ResolveJoinOfEventStoredAt(first_generation, second_generation);

    [Fact] void should_release_the_stored_event_with_the_schema_of_the_generation_it_was_stored_at() => _eventCompliance.Received(1).Release(_storedEvent, _storedSchema);
    [Fact] void should_use_the_released_event_as_the_resolved_join_source() => _results.Single().Event.ShouldEqual(_releasedEvent);
}
