// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_after_release;

public class and_a_handler_is_requested : given.all_dependencies
{
    Exception _error;

    void Establish() => _reactors.Release();

    async Task Because() => _error = await Catch.Exception(() => _reactors.Register(
        "released",
        builder => builder.WithEventType(new EventType("orders", 1)),
        (_, _) => Task.CompletedTask));

    [Fact] void should_throw_object_disposed() => _error.ShouldBeOfExactType<ObjectDisposedException>();
    [Fact] void should_not_send_a_registration() => _services.Reactors.DidNotReceive().Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>());
}
