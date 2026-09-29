// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionEventContextExtensions.when_resolving_join;

public class and_the_schema_of_the_stored_generation_is_not_known : given.a_join_of_a_stored_event_with_pii
{
    void Because() => ResolveJoinOfEventStoredAt(first_generation, second_generation, storedSchemaKnown: false);

    [Fact] void should_release_the_stored_event_with_the_schema_the_projection_was_built_with() => _eventCompliance.Received(1).Release(_storedEvent, _projectionSchema);
}
