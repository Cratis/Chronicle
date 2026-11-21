// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ChronicleClient.when_opening_foreign_stores;

public class and_at_deployment_reconnects : when_opening_namespace_stores.given.a_client_with_seeds
{
    IEventStore _foreign;

    async Task Establish() => _foreign = await _client.GetEventStore("foreign", null, RegistrationMode.AtDeployment);

    async Task Because()
    {
        _options.RegistrationMode = RegistrationMode.Inline;
        await _foreign.RegisterAll();
        await _connection.Lifecycle.Disconnected();
        await _connection.Lifecycle.Connected();
        await _foreign.RegisterAll();
    }

    [Fact] void should_not_provision_store() => _services.EventStores.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_register_types() => _services.EventTypes.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_register_read_models() => _services.ReadModels.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_seed_events() => _requests.ShouldBeEmpty();
}
