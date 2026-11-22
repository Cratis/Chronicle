// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.EventTypes.for_EventTypes.when_registering_after_discovery_changes;

public class and_an_event_type_was_added : given.an_event_types_with_multiple_stores
{
    async Task Because()
    {
        await _eventTypes.DiscoverAndRegister(_storeA);
        _discovered = [.. _discovered, typeof(BasicAuthorizationSetForWebhook)];
        await _eventTypes.DiscoverAndRegister(_storeB);
    }

    [Fact] void should_reuse_the_known_schema() => _preparations[typeof(WebhookAdded)].ShouldEqual(1);
    [Fact] void should_prepare_the_new_schema() => _preparations[typeof(BasicAuthorizationSetForWebhook)].ShouldEqual(1);
    [Fact] async Task should_register_the_known_type_in_the_new_store() => await ShouldRegister(_storeB, typeof(WebhookAdded));
    [Fact] async Task should_register_the_newly_discovered_type() => await ShouldRegister(_storeB, typeof(BasicAuthorizationSetForWebhook));
    [Fact] void should_resolve_the_new_type() => _eventTypes.GetClrTypeFor(typeof(BasicAuthorizationSetForWebhook).GetEventType().Id).ShouldEqual(typeof(BasicAuthorizationSetForWebhook));
}
