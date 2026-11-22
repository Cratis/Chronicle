// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ChronicleClient.when_opening_foreign_stores;

public class with_automatic_registration_disabled : when_opening_namespace_stores.given.a_client_with_seeds
{
    void Establish() => _options.AutoDiscoverAndRegister = false;

    async Task Because()
    {
        await _client.GetEventStore("foreign", null, RegistrationMode.EventTypesOnly);
        await _connection.Lifecycle.Disconnected();
        await _connection.Lifecycle.Connected();
    }

    [Fact] void should_not_ensure_store() => _services.EventStores.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_register_types() => _services.EventTypes.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_enable_automatic_registration() => _options.AutoDiscoverAndRegister.ShouldBeFalse();
}
