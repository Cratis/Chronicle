// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates.for_AggregateRootMutation.when_applying;

public class a_valid_event_and_auto_commit_not_enabled : given.an_aggregate_mutation
{
    [EventType]
    class SomeEvent;

    SomeEvent _event;

    void Establish()
    {
        _event = new();
    }

    Task Because() => _mutation.Apply(_event);

    [Fact] void should_call_mutator() => _mutator.Received().Mutate(Arg.Any<SomeEvent>());
    [Fact] void should_have_the_event_in_uncommitted_events() => _mutation.UncommittedEvents.ShouldContainOnly(_event);
    [Fact] void should_not_auto_commit() => _mutator.DidNotReceive().Dehydrate();
}
