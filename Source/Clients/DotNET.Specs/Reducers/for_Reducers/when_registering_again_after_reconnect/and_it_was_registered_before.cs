// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reducers;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering_again_after_reconnect;

public class and_it_was_registered_before : given.a_registered_reducer_that_was_disconnected
{
    Task Because() => _reducers.RegisterAgainAfterReconnect();

    [Fact] void should_register_the_reducer_on_the_new_connection() => _services.Reducers.Received(2).Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>());
    [Fact] void should_hand_out_a_recreated_handler() => ReferenceEquals(_reducers.GetHandlerFor(typeof(First)), _original).ShouldBeFalse();
    [Fact] void should_not_cancel_the_recreated_handler() => _reducers.GetHandlerFor(typeof(First)).CancellationToken.IsCancellationRequested.ShouldBeFalse();
}
