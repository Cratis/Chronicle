// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering;

public class and_keep_alive_is_skipped : given.all_dependencies
{
    Exception _error;

    void Establish()
    {
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor, IConnectionKeepAlive>();
        ((IChronicleServicesAccessor)connection).Services.Returns(_services);
        ((IConnectionKeepAlive)connection).IsSkipped.Returns(true);
        _eventStore.Connection.Returns(connection);

        var handler = Substitute.For<IReducerHandler>();
        handler.Id.Returns(new ReducerId("skipped-reducer"));
        handler.ReducerType.Returns(typeof(object));
        handler.ReadModelType.Returns(typeof(object));
        handler.IsActive.Returns(true);
        handler.EventTypes.Returns([new EventType("person-registered", 1)]);
        _handlersByModelType[typeof(object)] = handler;
    }

    async Task Because() => _error = await Catch.Exception(_reducers.Register);

    [Fact] void should_require_keep_alive() => _error.ShouldBeOfExactType<ClientObserversRequireKeepAlive>();
    [Fact] void should_name_the_reducer() => _error.Message.Contains("skipped-reducer").ShouldBeTrue();
    [Fact] void should_not_send_anything_to_the_kernel() => _observers.ReceivedCalls().Count().ShouldEqual(0);
}
