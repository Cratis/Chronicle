// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_ReactorMediator.when_calling_on_next;

public class with_same_reactor_and_connection_in_different_namespaces : given.a_reactor_mediator
{
    bool _firstObserverCalled;
    bool _secondObserverCalled;
    TaskCompletionSource<ObserverSubscriberResult> _taskCompletionSource;
    ObserverSubscriberResult _result;

    void Establish()
    {
        _mediator.Subscribe(
            new ReactorId("reactor"),
            new ConnectionId("connection"),
            new EventStoreName("store"),
            new EventStoreNamespaceName("namespace-a"),
            (_, _, tcs) =>
            {
                _firstObserverCalled = true;
                tcs.SetResult(ObserverSubscriberResult.Ok(EventSequenceNumber.First));
            });

        _mediator.Subscribe(
            new ReactorId("reactor"),
            new ConnectionId("connection"),
            new EventStoreName("store"),
            new EventStoreNamespaceName("namespace-b"),
            (_, _, tcs) =>
            {
                _secondObserverCalled = true;
                tcs.SetResult(ObserverSubscriberResult.Ok(EventSequenceNumber.First));
            });

        _taskCompletionSource = new TaskCompletionSource<ObserverSubscriberResult>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    async Task Because()
    {
        _mediator.OnNext(
            new ReactorId("reactor"),
            new ConnectionId("connection"),
            new EventStoreName("store"),
            new EventStoreNamespaceName("namespace-b"),
            new Key("partition", ArrayIndexers.NoIndexers),
            [],
            _taskCompletionSource);

        _result = await _taskCompletionSource.Task;
    }

    [Fact] void should_call_matching_observer() => _secondObserverCalled.ShouldBeTrue();
    [Fact] void should_not_call_observer_for_other_namespace() => _firstObserverCalled.ShouldBeFalse();
    [Fact] void should_return_success_result() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
}