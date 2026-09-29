// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionEventContextExtensions.when_resolving_join;

public class and_the_stored_generation_is_joined_more_than_once : given.a_join_of_a_stored_event_with_pii
{
    void Because()
    {
        ResolveJoinOfEventStoredAt(first_generation, second_generation);
        PublishEventThatNeedsTheJoin();
    }

    [Fact] void should_resolve_the_join_every_time() => _results.Count.ShouldEqual(2);
    [Fact] void should_release_every_stored_event() => _eventCompliance.Received(2).Release(_storedEvent, _storedSchema);
    [Fact] void should_only_load_the_schema_of_the_stored_generation_once() => _eventTypes.Received(1).GetFor(Arg.Any<IEnumerable<EventType>>());
}
