// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reducers;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering_again_after_reconnect;

public class and_a_connection_changes_during_register_all : given.a_registration_crossing_a_connection_change
{
    Task Because() => CrossConnectionChange();

    protected override Task RegisterAcrossConnectionChange() => _reducers.Register();

    [Fact] void should_complete_registration() => _error.ShouldBeNull();
    [Fact] void should_not_register_using_stale_services() => _services.Reducers.Received(1).Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>());
    [Fact] void should_keep_the_registration_on_the_current_connection() => _replacement.Reducers.Received(1).Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>());
    [Fact] void should_resolve_services_again() => _servicesReads.ShouldEqual(3);
}
