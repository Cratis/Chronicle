// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Aggregates.for_ReducerAggregateRootStateProvider;

public class when_updating_with_new_events : given.an_aggregate_root_that_handles_two_event_types
{
    StateForAggregateRoot _initialState;
    StateForAggregateRoot _state;
    StateForAggregateRoot _result;

    IEnumerable<object> _events;

    StateForAggregateRoot _initialStateInvokedWith;
    IEnumerable<object> _eventsInvokedWith;


    void Establish()
    {
        _initialState = new(Guid.NewGuid().ToString());
        _state = new(Guid.NewGuid().ToString());

        _events =
        [
            AppendedEvent.EmptyWithEventType(FirstEventType.EventTypeId),
            AppendedEvent.EmptyWithEventType(SecondEventType.EventTypeId)
        ];

        _invoker
            .Invoke(Arg.Any<IServiceProvider>(), Arg.Any<IEnumerable<EventAndContext>>(), Arg.Any<object>())
            .Returns(callInfo =>
            {
                var ev = callInfo.ArgAt<IEnumerable<EventAndContext>>(1);
                var initial = callInfo.ArgAt<object?>(2);
                _initialStateInvokedWith = initial as StateForAggregateRoot;
                _eventsInvokedWith = ev.Select(_ => _.Event);
                return new ReduceResult(_state, EventSequenceNumber.Unavailable, [], string.Empty);
            });
    }

    async Task Because() => _result = await _provider.Update(_initialState, _events);

    [Fact] void should_invoke_the_reducer_with_the_events() => _eventsInvokedWith.ShouldEqual(_events);
    [Fact] void should_invoke_the_reducer_with_the_initial_state() => _initialStateInvokedWith.ShouldEqual(_initialState);
    [Fact] void should_return_the_state() => _result.ShouldEqual(_state);
}
