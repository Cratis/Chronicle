// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

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
    [Fact] void should_not_send_anything_to_the_kernel() => _observers.ReceivedCalls().Count().ShouldEqual(0);
}
