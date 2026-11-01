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
    IReducerHandler _first;
    IReducerHandler _second;

    void Establish()
    {
        _first = AddHandler(typeof(object), "released-first");
        _second = AddHandler(typeof(string), "released-second");
        _eventStore.Connection.Lifecycle.ConnectionId.Returns(ConnectionId.New());
        _services.Reducers.Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>())
            .Returns(Observable.Empty<ReduceOperationMessage>());
    }

    async Task Because()
    {
        _reducers.Release();
        _reducers.Release();
        _eventStore.Connection.Lifecycle.OnDisconnected += Raise.Event<Disconnected>();
        await _reducers.Register();
    }

    [Fact] void should_disconnect_the_first_handler_once() => _first.Received(1).Disconnect();
    [Fact] void should_disconnect_the_second_handler_once() => _second.Received(1).Disconnect();
    [Fact] void should_dispose_the_first_handler_once() => ((IDisposable)_first).Received(1).Dispose();
    [Fact] void should_dispose_the_second_handler_once() => ((IDisposable)_second).Received(1).Dispose();
    [Fact] void should_empty_the_model_index() => _reducers.GetAllHandlers().ShouldBeEmpty();
    [Fact] void should_empty_the_first_type_index() => Catch.Exception(() => _reducers.GetHandlerFor(typeof(object))).ShouldBeOfExactType<UnknownReducerType>();
    [Fact] void should_empty_the_second_type_index() => Catch.Exception(() => _reducers.GetHandlerFor(typeof(string))).ShouldBeOfExactType<UnknownReducerType>();
    [Fact] void should_not_send_anything_to_the_kernel() => _services.Reducers.DidNotReceive().Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>());

    IReducerHandler AddHandler(Type type, string id)
    {
        var handler = Substitute.For<IReducerHandler, IDisposable>();
        handler.Id.Returns(new ReducerId(id));
        handler.ReducerType.Returns(type);
        handler.ReadModelType.Returns(type);
        handler.IsActive.Returns(true);
        handler.EventSequenceId.Returns(EventSequences.EventSequenceId.Log);
        handler.EventTypes.Returns([new EventType("spec-event", 1)]);
        _handlersByType[type] = handler;
        _handlersByModelType[type] = handler;
        return handler;
    }
}
