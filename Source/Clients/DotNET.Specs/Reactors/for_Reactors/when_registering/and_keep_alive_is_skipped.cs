// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering;

public class and_keep_alive_is_skipped : given.all_dependencies
{
    Exception _error;

    void Establish()
    {
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor, IConnectionKeepAlive>();
        ((IChronicleServicesAccessor)connection).Services.Returns(_services);
        ((IConnectionKeepAlive)connection).IsSkipped.Returns(true);
        connection.Lifecycle.Returns(_connectionLifecycle);
        _eventStore.Connection.Returns(connection);
    }

    async Task Because() => _error = await Catch.Exception(() => _reactors.Register(
        "skipped",
        builder => builder.WithEventType(new EventType("person-registered", 1)),
        (_, _) => Task.CompletedTask));

    [Fact] void should_require_keep_alive() => _error.ShouldBeOfExactType<ClientObserversRequireKeepAlive>();
    [Fact] void should_not_send_anything_to_the_kernel() => _services.Reactors.DidNotReceive().Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>());
}
