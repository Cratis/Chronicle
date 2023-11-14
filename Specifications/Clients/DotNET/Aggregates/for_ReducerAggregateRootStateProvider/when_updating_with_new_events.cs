// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Reducers;

namespace Aksio.Cratis.Aggregates.for_ReducerAggregateRootStateProvider;

public class when_updating_with_new_events : given.an_aggregate_root_that_handles_two_event_types
{
    StateForAggregateRoot state;
    StateForAggregateRoot result;

    IEnumerable<object> events;
    IEnumerable<object> events_invoked_with;


    void Establish()
    {
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
                events_invoked_with = ev.Select(_ => _.Event);
                return new InternalReduceResult(state, EventSequenceNumber.Unavailable);
            });
    }

    async Task Because() => result = await provider.Update(events) as StateForAggregateRoot;

    [Fact] void should_invoke_the_reducer_with_the_events() => events_invoked_with.ShouldEqual(events);
    [Fact] void should_return_the_state() => result.ShouldEqual(state);
}
