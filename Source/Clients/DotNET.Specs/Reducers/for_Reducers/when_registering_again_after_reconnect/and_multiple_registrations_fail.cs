// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reducers;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering_again_after_reconnect;

public class and_multiple_registrations_fail : given.two_registered_reducers_that_were_disconnected
{
    Exception _error;

    void Establish() => _services.Reducers.When(_ => _.Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>()))
        .Do(_ => throw new InvalidOperationException("registration failed"));

    async Task Because() => _error = await Catch.Exception(_reducers.RegisterAgainAfterReconnect);

    [Fact] void should_surface_all_failures() => ((AggregateException)_error).InnerExceptions.Count.ShouldEqual(2);
}
