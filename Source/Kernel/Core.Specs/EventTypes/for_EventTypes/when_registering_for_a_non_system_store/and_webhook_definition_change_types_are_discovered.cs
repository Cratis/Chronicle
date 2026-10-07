// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventTypes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.EventTypes.for_EventTypes.when_registering_for_a_non_system_store;

/// <summary>
/// Changing a registered webhook appends these events to the system sequence of the store that owns the webhook, so
/// every store needs their schemas - not only the System store (Cratis/Chronicle#4567).
/// </summary>
public class and_webhook_definition_change_types_are_discovered : Specification
{
    EventTypes _eventTypes;
    IEventTypesStorage _eventTypesStorage;

    void Establish()
    {
        var types = Substitute.For<ITypes>();
        types.All.Returns([
            typeof(EventTypesSetForWebhook),
            typeof(TargetUrlSetForWebhook),
            typeof(TargetHeadersSetForWebhook),
            typeof(WebhookRemoved)]);
        var storage = Substitute.For<IStorage>();
        _eventTypesStorage = Substitute.For<IEventTypesStorage>();
        storage.GetEventStore("non-system-store").EventTypes.Returns(_eventTypesStorage);
        _eventTypes = new(types, storage, NullLogger<EventTypes>.Instance);
    }

    async Task Because() => await _eventTypes.DiscoverAndRegister("non-system-store");

    [Fact] async Task should_register_the_event_types_set_schema() => await ShouldRegister(typeof(EventTypesSetForWebhook));
    [Fact] async Task should_register_the_target_url_set_schema() => await ShouldRegister(typeof(TargetUrlSetForWebhook));
    [Fact] async Task should_register_the_target_headers_set_schema() => await ShouldRegister(typeof(TargetHeadersSetForWebhook));
    [Fact] async Task should_register_the_webhook_removed_schema() => await ShouldRegister(typeof(WebhookRemoved));

    async Task ShouldRegister(Type type) => await _eventTypesStorage.Received(1).Register(
        type.GetEventType(),
        Arg.Any<JsonSchema>(),
        EventTypeOwner.Server,
        EventTypeSource.Code);
}
