// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reducers;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering_again_after_reconnect;

public class and_registering_a_type_connects : given.a_registration_that_connects
{
    Task Because() => RegisterAndAwaitConnection();

    protected override Task RegisterWhileDisconnected() => _reducers.Register(typeof(Second));

    [Fact] void should_complete_without_blocking_the_connected_handler() => _error.ShouldBeNull();
    [Fact] void should_register_each_reducer_once_on_the_new_connection() => _services.Reducers.Received(3).Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>());
}
