// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class with_the_delivery_policy_and_pins_changed : given.a_pinned_registration
{
    void Establish() => ExistingDefinition(true, true);
    async Task Because() => await _registrar.Add("store", [_definition]);
    [Fact] void should_record_both_atomically() => _appended.OfType<GenerationDeliverySetForWebhook>().Single().EventTypes.ShouldContainOnly(new EventType("customer", 3));
    [Fact] void should_not_append_an_intermediate_pin_change() => _appended.OfType<EventTypesSetForWebhook>().ShouldBeEmpty();
}
