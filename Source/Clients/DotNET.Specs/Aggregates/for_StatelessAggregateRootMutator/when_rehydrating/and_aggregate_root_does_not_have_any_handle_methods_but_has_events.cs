// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates.for_StatelessAggregateRootMutator.when_rehydrating;

public class and_aggregate_root_does_not_have_any_handle_methods_but_has_events : given.a_stateless_aggregate_root_mutator
{
    void Establish()
    {
        _eventSequence.HasEventsFor(_eventSourceId).Returns(true);
    }

    async Task Because() => await _mutator.Rehydrate();

    [Fact] void should_not_get_any_events() => _eventSequence.DidNotReceive().GetForEventSourceIdAndEventTypes(_eventSourceId, Arg.Any<IEnumerable<EventType>>());
    [Fact] void should_not_handle_any_events() => _eventHandlers.DidNotReceive().Handle(Arg.Any<IAggregateRoot>(), Arg.Any<IEnumerable<EventAndContext>>());
    [Fact] void should_ask_if_there_are_events_for_the_event_source_id() => _eventSequence.Received(1).HasEventsFor(_eventSourceId);
    [Fact] void should_set_has_events_to_true() => _aggregateRootContext.HasEvents.ShouldBeTrue();
}
