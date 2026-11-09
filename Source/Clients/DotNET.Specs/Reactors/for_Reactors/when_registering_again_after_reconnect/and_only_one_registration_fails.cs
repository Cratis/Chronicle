// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reactors;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_again_after_reconnect;

public class and_only_one_registration_fails : given.two_registered_delegates_that_were_disconnected
{
    readonly Exception _failure = new InvalidOperationException("registration failed");
    Exception _error;
    int _attempts;
    int _afterFirstPass;

    void Establish() => _services.Reactors.When(_ => _.Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>()))
        .Do(_ =>
        {
            if (++_attempts == 1)
            {
                throw _failure;
            }
        });

    async Task Because()
    {
        _error = await Catch.Exception(_reactors.RegisterAgainAfterReconnect);
        _afterFirstPass = _definitions.Count;
        await _reactors.RegisterAgainAfterReconnect();
    }

    [Fact] void should_surface_the_failure() => _error.ShouldEqual(_failure);
    [Fact] void should_register_the_other_reactor() => _afterFirstPass.ShouldEqual(3);
    [Fact] void should_retry_only_the_failed_registration() => _attempts.ShouldEqual(3);
    [Fact] void should_register_both_reactors_eventually() => _definitions.Count.ShouldEqual(4);
}
