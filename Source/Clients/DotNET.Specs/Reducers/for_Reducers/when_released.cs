// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers;

public class when_released : given.all_dependencies
{
    IReducerHandler _handler;

    void Establish()
    {
        _handler = Substitute.For<IReducerHandler>();
        _handler.Id.Returns(new ReducerId("released-reducer"));
        _handler.ReducerType.Returns(typeof(object));
        _handler.ReadModelType.Returns(typeof(object));
        _handler.IsActive.Returns(true);
        _handler.EventSequenceId.Returns(EventSequences.EventSequenceId.Log);
        _handler.EventTypes.Returns([new EventType("spec-event", 1)]);
        _eventStore.Connection.Lifecycle.ConnectionId.Returns(ConnectionId.New());
        _services.Reducers.Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>())
            .Returns(Observable.Empty<ReduceOperationMessage>());
        _handlersByType[typeof(object)] = _handler;
        _handlersByModelType[typeof(object)] = _handler;
    }

    async Task Because()
    {
        _reducers.Release();
        _eventStore.Connection.Lifecycle.OnDisconnected += Raise.Event<Disconnected>();
        await _reducers.Register();
    }

    [Fact] void should_disconnect_the_handler_once() => _handler.Received(1).Disconnect();
    [Fact] void should_not_send_anything_to_the_kernel() => _services.Reducers.DidNotReceive().Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>());
}
