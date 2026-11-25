// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ChronicleClient.when_opening_foreign_stores;

public class with_at_deployment : given.a_client_with_an_own_store
{
    async Task Because()
    {
        _foreign = await _client.GetEventStore("foreign", null, RegistrationMode.AtDeployment);
        await _foreign.RegisterAll();
    }

    [Fact] void should_not_provision_foreign_store() => _services.EventStores.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_register_foreign_event_types() => _services.EventTypes.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_register_foreign_artifacts() => ShouldNotRegisterForeignArtifacts();
    [Fact] void should_keep_local_event_identity() => _foreign.EventTypes.GetEventTypeFor(typeof(PlainEvent)).Id.Value.ShouldEqual("plain");
    [Fact] void should_not_change_global_mode() => _options.RegistrationMode.ShouldEqual(RegistrationMode.Inline);
}
