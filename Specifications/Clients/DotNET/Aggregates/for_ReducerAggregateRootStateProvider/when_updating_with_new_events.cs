// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Reducers;

namespace Aksio.Cratis.Aggregates.for_ReducerAggregateRootStateProvider;

public class when_updating_with_new_events : given.an_aggregate_root_that_handles_two_event_types
{
    StateForAggregateRoot initial_state;
    StateForAggregateRoot state;
    StateForAggregateRoot result;

    IEnumerable<object> events;

    StateForAggregateRoot initial_state_invoked_with;
    IEnumerable<object> events_invoked_with;


    void Establish()
    {
        initial_state = new(Guid.NewGuid().ToString());
        state = new(Guid.NewGuid().ToString());

        events = new[]
        {
            AppendedEvent.EmptyWithEventType(FirstEventType.EventTypeId),
            AppendedEvent.EmptyWithEventType(SecondEventType.EventTypeId)
        };

        invoker
            .Setup(_ => _.Invoke(IsAny<IEnumerable<EventAndContext>>(), IsAny<object>()))
            .ReturnsAsync((IEnumerable<EventAndContext> ev, object? initial) =>
            {
                initial_state_invoked_with = initial as StateForAggregateRoot;
                events_invoked_with = ev.Select(_ => _.Event);
                return new InternalReduceResult(state, EventSequenceNumber.Unavailable, Enumerable.Empty<string>(), string.Empty);
            });
    }

    async Task Because() => result = await provider.Update(initial_state, events) as StateForAggregateRoot;

    [Fact] void should_invoke_the_reducer_with_the_events() => events_invoked_with.ShouldEqual(events);
    [Fact] void should_invoke_the_reducer_with_the_initial_state() => initial_state_invoked_with.ShouldEqual(initial_state);
    [Fact] void should_return_the_state() => result.ShouldEqual(state);
}
