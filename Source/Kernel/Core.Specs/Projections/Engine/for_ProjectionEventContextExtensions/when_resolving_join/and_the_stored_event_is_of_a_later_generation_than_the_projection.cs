// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionEventContextExtensions.when_resolving_join;

public class and_the_stored_event_is_of_a_later_generation_than_the_projection : given.a_join_of_a_stored_event_with_pii
{
    void Establish() => _storedSchema = new JsonSchema();

    void Because() => ResolveJoinOfEventStoredAt(second_generation, first_generation);

    [Fact] void should_not_release_with_the_schema_the_projection_was_built_with() => _eventCompliance.DidNotReceive().Release(Arg.Any<AppendedEvent>(), _projectionSchema);
    [Fact] void should_not_release_when_the_stored_generation_has_no_compliance_metadata() => _eventCompliance.DidNotReceive().Release(Arg.Any<AppendedEvent>(), Arg.Any<JsonSchema>());
    [Fact] void should_still_use_the_stored_event_as_the_resolved_join_source() => _results.Single().Event.ShouldEqual(_storedEvent);
}
