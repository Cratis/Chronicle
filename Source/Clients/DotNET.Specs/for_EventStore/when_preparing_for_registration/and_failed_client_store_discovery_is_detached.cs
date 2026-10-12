// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.for_EventStore.when_preparing_for_registration;

public class and_failed_client_store_discovery_is_detached : given.an_explicit_store
{
    ChronicleClient _client;
    Exception _failure;
    Exception _reconnectFailure;

    void Establish()
    {
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>(_ => _.WithId("same")).RegisterEventType<AnotherEvent>(_ => _.WithId("same"));
        _client = new(_connection, _options, _artifacts, _provider);
    }

    async Task Because()
    {
        _failure = await Catch.Exception(() => _client.GetEventStore("current", "default"));
        _reconnectFailure = await Catch.Exception(_connection.Lifecycle.Connected);
    }

    void Destroy() => _client.Dispose();

    [Fact] void should_surface_the_original_declaration_failure() => _failure.ShouldBeOfExactType<MultipleEventTypesWithSameIdFound>();
    [Fact] void should_not_reenter_the_evicted_failed_store_on_reconnect() => _reconnectFailure.ShouldBeNull();
    [Fact] void should_not_send_registration_effects_for_the_failed_store() => _services.EventStores.ReceivedCalls().ShouldBeEmpty();
}
