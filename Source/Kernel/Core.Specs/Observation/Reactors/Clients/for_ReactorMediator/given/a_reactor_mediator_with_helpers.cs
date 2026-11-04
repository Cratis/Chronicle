// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_ReactorMediator.given;

public class a_reactor_mediator_with_helpers : a_reactor_mediator
{
    protected ReactorEventsObserver _first;
    protected ReactorEventsObserver _second;
    protected ReactorReplayObserver _firstReplay;
    protected ReactorReplayObserver _secondReplay;
    protected int _firstCalls;
    protected int _secondCalls;
    protected bool _ownsRegistration;

    void Establish()
    {
        _first = (_, _, _, _, _) => _firstCalls++;
        _second = (_, _, _, _, _) => _secondCalls++;
        _firstReplay = (_, _) => { };
        _secondReplay = (_, _) => { };
    }

    protected void Register(ReactorEventsObserver target, ReactorReplayObserver replayTarget)
    {
        _mediator.Subscribe(new ReactorId("reactor"), new ConnectionId("connection"), new EventStoreName("store"), new EventStoreNamespaceName("namespace"), target);
        _mediator.SubscribeReplayNotifications(new ReactorId("reactor"), new ConnectionId("connection"), new EventStoreName("store"), new EventStoreNamespaceName("namespace"), replayTarget);
    }

    protected bool Disconnect(ReactorEventsObserver target, ReactorReplayObserver replayTarget) =>
        _mediator.Disconnected(new ReactorId("reactor"), new ConnectionId("connection"), new EventStoreName("store"), new EventStoreNamespaceName("namespace"), target, replayTarget);

    protected ObserverSubscriberState Deliver()
    {
        var tcs = new TaskCompletionSource<ObserverSubscriberResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _mediator.OnNext(new ReactorId("reactor"), new ConnectionId("connection"), new EventStoreName("store"), new EventStoreNamespaceName("namespace"), new Key("partition", ArrayIndexers.NoIndexers), [], tcs);
        return tcs.Task.IsCompleted ? tcs.Task.Result.State : ObserverSubscriberState.Ok;
    }
}
