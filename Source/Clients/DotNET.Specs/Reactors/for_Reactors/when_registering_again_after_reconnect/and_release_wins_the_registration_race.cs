// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reactors;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_again_after_reconnect;

public class and_release_wins_the_registration_race : given.a_registration_crossing_a_connection_change
{
    Task Because() => CrossConnectionChange();

    protected override Task RegisterAcrossConnectionChange() => _reactors.Register<AnotherReactor>();

    protected override Task WhileServicesAreResolved()
    {
        _reactors.Release();
        return Task.CompletedTask;
    }

    [Fact] void should_refuse_the_registration() => _error.ShouldBeOfExactType<ObjectDisposedException>();
    [Fact] void should_not_register_after_release() => _services.Reactors.Received(1).Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>());
    [Fact] void should_not_retry_after_release() => _servicesReads.ShouldEqual(1);
}
