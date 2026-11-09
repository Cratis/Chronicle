// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reducers;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering_again_after_reconnect;

public class and_registering_fails : given.a_registered_reducer_that_was_disconnected
{
    readonly Exception _failure = new InvalidOperationException("registration failed");
    Exception _error;
    int _attempts;

    void Establish() => _services.Reducers.When(_ => _.Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>()))
        .Do(_ =>
        {
            if (++_attempts == 1)
            {
                throw _failure;
            }
        });

    async Task Because()
    {
        _error = await Catch.Exception(_reducers.RegisterAgainAfterReconnect);
        await _reducers.RegisterAgainAfterReconnect();
    }

    [Fact] void should_surface_the_failure() => _error.ShouldEqual(_failure);
    [Fact] void should_retry_on_the_next_call() => _attempts.ShouldEqual(2);
}
