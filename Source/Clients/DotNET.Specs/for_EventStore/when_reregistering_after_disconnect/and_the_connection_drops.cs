// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_EventStore.when_reregistering_after_disconnect;

public class and_the_connection_drops : given.a_registered_store
{
    async Task Because() => await _connection.Lifecycle.Disconnected();

    [Fact] void should_mark_registration_pending_for_reconnect() => _store.Registration.IsRegistering.ShouldBeTrue();
    [Fact] void should_preserve_the_previous_outcome() => (_store.Registration with { IsRegistering = false }).ShouldEqual(_previous);
}
