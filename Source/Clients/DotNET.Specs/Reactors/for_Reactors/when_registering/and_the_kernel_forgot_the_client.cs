// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;
using ContractReactors = Cratis.Chronicle.Contracts.Observation.Reactors.IReactors;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering;

public class and_the_kernel_forgot_the_client : given.all_dependencies
{
    int _observeCalls;

    void Establish()
    {
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _connectionLifecycle.ConnectionId.Returns((ConnectionId)"test-connection-id");

        var status = new Status(StatusCode.Unknown, "Exception was thrown by handler. ClientIsNotConnected: Client 'test-connection-id' is not connected.");
        var reactors = Substitute.For<ContractReactors>();
        _services.Reactors.Returns(reactors);
        reactors.Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>())
            .Returns(_ =>
            {
                _observeCalls++;
                return Observable.Throw<EventsToObserve>(new RpcException(status));
            });
        _eventStore.EventTypes.Returns(_eventTypes);
        _eventTypes.AllClrTypes.Returns([]);
        _reactors.Register<MyReactor>().GetAwaiter().GetResult();
    }

    async Task Because() => await _reactors.Register();

    [Fact] void should_ask_the_connection_to_reconnect() => _eventStore.Connection.Received(1).RequestReconnect();
    [Fact] void should_not_retry_the_registration_with_the_stale_connection() => _observeCalls.ShouldEqual(1);

    class MyReactor : IReactor;
}
