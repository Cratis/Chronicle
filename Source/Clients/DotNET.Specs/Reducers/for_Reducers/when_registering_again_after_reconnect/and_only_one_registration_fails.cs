// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reducers;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering_again_after_reconnect;

public class and_only_one_registration_fails : given.two_registered_reducers_that_were_disconnected
{
    readonly Exception _failure = new InvalidOperationException("registration failed");
    Exception _error;
    int _attempts;
    int _afterFirstPass;

    void Establish() => _services.Reducers.When(_ => _.Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>()))
        .Do(_ =>
        {
            if (Interlocked.Increment(ref _attempts) == 1)
            {
                throw _failure;
            }
        });

    async Task Because()
    {
        _error = await Catch.Exception(_reducers.RegisterAgainAfterReconnect);
        _afterFirstPass = _attempts;
        await _reducers.RegisterAgainAfterReconnect();
    }

    [Fact] void should_surface_the_failure() => _error.ShouldEqual(_failure);
    [Fact] void should_still_register_the_other_reducer() => _afterFirstPass.ShouldEqual(2);
    [Fact] void should_retry_only_the_failed_registration() => _attempts.ShouldEqual(3);
}
