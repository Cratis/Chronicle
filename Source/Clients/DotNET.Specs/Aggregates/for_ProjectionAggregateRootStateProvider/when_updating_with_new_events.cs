// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Aggregates.for_ProjectionAggregateRootStateProvider;

public class when_updating_with_new_events : given.an_aggregate_root_that_handles_two_event_types
{
    StateForAggregateRoot _initialState;
    StateForAggregateRoot _state;
    StateForAggregateRoot _result;

    IEnumerable<object> _events;

    IEnumerable<object> _eventsInvokedWith;
    ProjectionResult _projectionResult;

    void Establish()
    {
        _initialState = new(Guid.NewGuid().ToString());
        _state = new(Guid.NewGuid().ToString());
        _projectionResult = new(_state, [], 0, 42);

        _events =
        [
            AppendedEvent.EmptyWithEventType(FirstEventType.EventTypeId),
            AppendedEvent.EmptyWithEventType(SecondEventType.EventTypeId)
        ];

        _projections
            .GetInstanceByIdForSessionWithEventsApplied(_correlationId, typeof(StateForAggregateRoot), _eventSourceId, _events)
            .Returns(callInfo =>
            {
                _eventsInvokedWith = callInfo.ArgAt<IEnumerable<object>>(3);
                return _projectionResult;
            });
    }

    async Task Because() => _result = await _provider.Update(_initialState, _events);

    [Fact] void should_invoke_the_projection_with_the_events() => _eventsInvokedWith.ShouldEqual(_events);
    [Fact] void should_return_the_state() => _result.ShouldEqual(_state);
    [Fact] void should_set_the_tail_event_sequence_number() => _aggregateRootContext.TailEventSequenceNumber.ShouldEqual(_projectionResult.LastHandledEventSequenceNumber);
}
