// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_reregistering_after_disconnect.given;

public class a_registered_store : when_preparing_for_registration.given.an_explicit_store
{
    protected RegistrationOutcome _previous;

    async Task Establish()
    {
        _auto = true;
        _store = CreateStore();
        await _connection.Lifecycle.Connected();
        _previous = _store.Registration;
        _services.ClearReceivedCalls();
        _services.EventStores.ClearReceivedCalls();
    }
}
