// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reactors;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_again_after_reconnect;

public class and_multiple_registrations_fail : given.two_registered_delegates_that_were_disconnected
{
    readonly Exception[] _failures = [new InvalidOperationException("first failure"), new InvalidOperationException("second failure")];
    Exception _error;
    int _attempts;

    void Establish() => _services.Reactors.When(_ => _.Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>()))
        .Do(_ => throw _failures[_attempts++]);

    async Task Because() => _error = await Catch.Exception(_reactors.RegisterAgainAfterReconnect);

    [Fact] void should_attempt_every_registration() => _attempts.ShouldEqual(2);
    [Fact] void should_throw_an_aggregate() => _error.ShouldBeOfExactType<AggregateException>();
    [Fact] void should_include_both_failures() => ((AggregateException)_error).InnerExceptions.ShouldContainOnly(_failures);
}
