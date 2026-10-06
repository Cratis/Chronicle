// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventTypes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.EventTypes.for_EventTypes.when_registering_for_a_non_system_store;

public class and_webhook_authorization_types_are_discovered : Specification
{
    EventTypes _eventTypes;
    IEventTypesStorage _eventTypesStorage;

    void Establish()
    {
        var types = Substitute.For<ITypes>();
        types.All.Returns([
            typeof(WebhookAdded),
            typeof(BasicAuthorizationSetForWebhook),
            typeof(BearerTokenAuthorizationSetForWebhook),
            typeof(OAuthAuthorizationSetForWebhook)]);
        var storage = Substitute.For<IStorage>();
        _eventTypesStorage = Substitute.For<IEventTypesStorage>();
        storage.GetEventStore("non-system-store").EventTypes.Returns(_eventTypesStorage);
        _eventTypes = new(types, storage, NullLogger<EventTypes>.Instance);
    }

    async Task Because() => await _eventTypes.DiscoverAndRegister("non-system-store");

    [Fact] async Task should_register_the_webhook_added_schema() => await ShouldRegister(typeof(WebhookAdded));
    [Fact] async Task should_register_the_basic_authorization_schema() => await ShouldRegister(typeof(BasicAuthorizationSetForWebhook));
    [Fact] async Task should_register_the_bearer_authorization_schema() => await ShouldRegister(typeof(BearerTokenAuthorizationSetForWebhook));
    [Fact] async Task should_register_the_oauth_authorization_schema() => await ShouldRegister(typeof(OAuthAuthorizationSetForWebhook));

    async Task ShouldRegister(Type type) => await _eventTypesStorage.Received(1).Register(
        type.GetEventType(),
        Arg.Any<JsonSchema>(),
        EventTypeOwner.Server,
        EventTypeSource.Code);
}
