// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerMediator.given;

public class a_reducer_mediator_with_helpers : a_reducer_mediator
{
    protected ReducerEventsObserver _first;
    protected ReducerEventsObserver _second;
    protected ReducerReplayObserver _firstReplay;
    protected ReducerReplayObserver _secondReplay;
    protected int _firstCalls;
    protected int _secondCalls;
    protected bool _ownsRegistration;

    void Establish()
    {
        _first = (_, _) => _firstCalls++;
        _second = (_, _) => _secondCalls++;
        _firstReplay = (_, _) => { };
        _secondReplay = (_, _) => { };
    }

    protected void Register(ReducerEventsObserver target, ReducerReplayObserver replayTarget)
    {
        _mediator.Subscribe(new ReducerId("reducer"), new ConnectionId("connection"), new EventStoreName("store"), new EventStoreNamespaceName("namespace"), target);
        _mediator.SubscribeReplayNotifications(new ReducerId("reducer"), new ConnectionId("connection"), new EventStoreName("store"), new EventStoreNamespaceName("namespace"), replayTarget);
    }

    protected bool Disconnect(ReducerEventsObserver target, ReducerReplayObserver replayTarget) =>
        _mediator.Disconnected(new ReducerId("reducer"), new ConnectionId("connection"), new EventStoreName("store"), new EventStoreNamespaceName("namespace"), target, replayTarget);

    protected ObserverSubscriberState Deliver()
    {
        var tcs = new TaskCompletionSource<ReducerSubscriberResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _mediator.OnNext(new ReducerId("reducer"), new ConnectionId("connection"), new EventStoreName("store"), new EventStoreNamespaceName("namespace"), new ReduceOperation(new Key("partition", ArrayIndexers.NoIndexers), [], null), tcs);
        return tcs.Task.IsCompleted ? tcs.Task.Result.ObserverResult.State : ObserverSubscriberState.Ok;
    }
}
