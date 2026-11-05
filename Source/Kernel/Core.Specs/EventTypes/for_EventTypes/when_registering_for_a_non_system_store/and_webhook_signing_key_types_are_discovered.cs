// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Webhooks.Signing;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventTypes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.EventTypes.for_EventTypes.when_registering_for_a_non_system_store;

public class and_webhook_signing_key_types_are_discovered : Specification
{
    EventTypes _eventTypes;
    IEventTypesStorage _eventTypesStorage;

    void Establish()
    {
        var types = Substitute.For<ITypes>();
        types.All.Returns([typeof(WebhookSigningKeyGenerated), typeof(WebhookSigningKeySuperseded), typeof(WebhookSigningKeyRetired)]);
        var storage = Substitute.For<IStorage>();
        _eventTypesStorage = Substitute.For<IEventTypesStorage>();
        storage.GetEventStore("non-system-store").EventTypes.Returns(_eventTypesStorage);
        _eventTypes = new(types, storage, NullLogger<EventTypes>.Instance);
    }

    async Task Because() => await _eventTypes.DiscoverAndRegister("non-system-store");

    [Fact] async Task should_register_the_generated_schema() => await ShouldRegister(typeof(WebhookSigningKeyGenerated));
    [Fact] async Task should_register_the_superseded_schema() => await ShouldRegister(typeof(WebhookSigningKeySuperseded));
    [Fact] async Task should_register_the_retired_schema() => await ShouldRegister(typeof(WebhookSigningKeyRetired));

    async Task ShouldRegister(Type type) => await _eventTypesStorage.Received(1).Register(
        type.GetEventType(),
        Arg.Any<JsonSchema>(),
        EventTypeOwner.Server,
        EventTypeSource.Code);
}
