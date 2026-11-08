// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reactors;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.for_EventStore.when_reregistering_after_disconnect;

public class and_automatic_registration_is_off_and_registering_again_fails : given.a_store_with_an_explicit_reactor
{
    readonly InvalidOperationException _failure = new("the kernel refused the registration");
    Exception _error;

    void Establish() =>
        _services.Reactors.Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>()).Returns(_ => throw _failure);

    async Task Because()
    {
        await _connection.Lifecycle.Disconnected();
        _error = await Catch.Exception(_connection.Lifecycle.Connected);
    }

    [Fact] void should_fail_the_connected_event_with_an_aggregate() => _error.ShouldBeAssignableFrom<AggregateException>();
    [Fact] void should_carry_the_registration_failure() => ((AggregateException)_error).InnerExceptions.ShouldContainOnly(_failure);
    [Fact] void should_not_be_connected() => _connection.Lifecycle.IsConnected.ShouldBeFalse();
}
