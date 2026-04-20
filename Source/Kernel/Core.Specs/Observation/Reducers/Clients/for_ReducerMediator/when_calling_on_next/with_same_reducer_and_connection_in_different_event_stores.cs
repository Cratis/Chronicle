// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerMediator.when_calling_on_next;

public class with_same_reducer_and_connection_in_different_event_stores : given.a_reducer_mediator
{
    bool _firstObserverCalled;
    bool _secondObserverCalled;
    TaskCompletionSource<ReducerSubscriberResult> _taskCompletionSource;
    ReducerSubscriberResult _result;

    void Establish()
    {
        _mediator.Subscribe(
            new ReducerId("reducer"),
            new ConnectionId("connection"),
            new EventStoreName("store-a"),
            EventStoreNamespaceName.Default,
            (_, tcs) =>
            {
                _firstObserverCalled = true;
                tcs.SetResult(new ReducerSubscriberResult(ObserverSubscriberResult.Ok(EventSequenceNumber.First), null));
            });

        _mediator.Subscribe(
            new ReducerId("reducer"),
            new ConnectionId("connection"),
            new EventStoreName("store-b"),
            EventStoreNamespaceName.Default,
            (_, tcs) =>
            {
                _secondObserverCalled = true;
                tcs.SetResult(new ReducerSubscriberResult(ObserverSubscriberResult.Ok(EventSequenceNumber.First), null));
            });

        _taskCompletionSource = new TaskCompletionSource<ReducerSubscriberResult>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    async Task Because()
    {
        _mediator.OnNext(
            new ReducerId("reducer"),
            new ConnectionId("connection"),
            new EventStoreName("store-b"),
            EventStoreNamespaceName.Default,
            new ReduceOperation(new Key("partition", ArrayIndexers.NoIndexers), [], null),
            _taskCompletionSource);

        _result = await _taskCompletionSource.Task;
    }

    [Fact] void should_call_matching_observer() => _secondObserverCalled.ShouldBeTrue();
    [Fact] void should_not_call_observer_for_other_event_store() => _firstObserverCalled.ShouldBeFalse();
    [Fact] void should_return_success_result() => _result.ObserverResult.State.ShouldEqual(ObserverSubscriberState.Ok);
}