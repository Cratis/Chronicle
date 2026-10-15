// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventStores;

namespace Cratis.Chronicle.for_EventStore.when_reregistering_after_disconnect;

public class and_automatic_registration_was_detached : given.a_registered_store
{
    void Establish() => _store.DetachAutomaticRegistration();

    async Task Because()
    {
        await _connection.Lifecycle.Disconnected();
        await _connection.Lifecycle.Connected();
    }

    [Fact] void should_not_mark_registration_pending() => _store.Registration.IsRegistering.ShouldBeFalse();
    [Fact] void should_not_register_on_reconnect() => _services.EventStores.DidNotReceive().EnsureEventStore(Arg.Any<EnsureEventStoreRequest>());
}
