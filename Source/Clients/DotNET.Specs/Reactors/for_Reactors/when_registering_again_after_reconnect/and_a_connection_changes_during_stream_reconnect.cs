// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reactors;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_again_after_reconnect;

public class and_a_connection_changes_during_stream_reconnect : given.a_registration_crossing_a_connection_change
{
    Task Because() => CrossConnectionChange();

    protected override Task RegisterAcrossConnectionChange()
    {
        _observed.OnCompleted();
        return _reconnectWork.Single()();
    }

    [Fact] void should_complete_the_reconnect_work() => _error.ShouldBeNull();
    [Fact] void should_not_register_using_stale_services() => _services.Reactors.Received(1).Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>());
    [Fact] void should_keep_the_registration_on_the_current_connection() => _replacement.Reactors.Received(1).Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>());
    [Fact] void should_resolve_services_again() => _servicesReads.ShouldEqual(3);
}
