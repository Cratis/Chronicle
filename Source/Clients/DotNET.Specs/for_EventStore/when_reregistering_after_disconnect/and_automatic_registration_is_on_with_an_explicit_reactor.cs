// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reactors;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.for_EventStore.when_reregistering_after_disconnect;

public class and_automatic_registration_is_on_with_an_explicit_reactor : given.a_store_with_an_explicit_reactor
{
    protected override bool AutomaticRegistration => true;

    async Task Because()
    {
        await _connection.Lifecycle.Disconnected();
        await _connection.Lifecycle.Connected();
    }

    [Fact] void should_register_only_once_on_the_new_connection() => _services.Reactors.Received(2).Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>());
}
