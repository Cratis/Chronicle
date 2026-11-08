// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reducers;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering_again_after_reconnect;

public class and_release_wins_the_registration_race : given.a_registration_crossing_a_connection_change
{
    Task Because() => CrossConnectionChange();

    protected override Task RegisterAcrossConnectionChange() => _reducers.Register(typeof(Second));

    protected override Task WhileServicesAreResolved()
    {
        _reducers.Release();
        return Task.CompletedTask;
    }

    [Fact] void should_refuse_the_registration() => _error.ShouldBeOfExactType<ObjectDisposedException>();
    [Fact] void should_not_register_after_release() => _services.Reducers.Received(1).Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>());
    [Fact] void should_not_retry_after_release() => _servicesReads.ShouldEqual(1);
}
