// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionEventContextExtensions.when_resolving_join;

public class and_the_stored_event_is_of_a_later_generation_than_the_projection : given.a_join_of_a_stored_event_with_pii
{
    void Establish() => _storedSchema = new JsonSchema();

    void Because() => ResolveJoinOfEventStoredAt(second_generation, first_generation);

    [Fact] void should_use_the_retained_schema_only_as_a_hint_for_current_effective_protection() => _eventCompliance.Received(1).Release(_storedEvent, _projectionSchema);
    [Fact] void should_not_treat_the_retained_stored_schema_as_effective_protection() => _eventCompliance.DidNotReceive().Release(Arg.Any<AppendedEvent>(), _storedSchema);
    [Fact] void should_use_the_event_returned_by_the_release_boundary() => _results.Single().Event.ShouldEqual(_releasedEvent);
}
