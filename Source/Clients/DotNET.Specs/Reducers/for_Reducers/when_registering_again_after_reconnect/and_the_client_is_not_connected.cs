// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reducers;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering_again_after_reconnect;

public class and_the_client_is_not_connected : given.a_registered_reducer_that_was_disconnected
{
    void Establish() => _lifecycle.IsConnected.Returns(false);

    Task Because() => _reducers.RegisterAgainAfterReconnect();

    [Fact] void should_not_register_the_reducer_again() => _services.Reducers.Received(1).Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>());
}
