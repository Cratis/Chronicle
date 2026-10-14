// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class with_only_the_pin_changed : given.a_pinned_registration
{
    void Establish() => ExistingDefinition(true, false);
    async Task Because() => await _registrar.Add("store", [_definition]);
    [Fact] void should_record_the_exact_pin() => _appended.OfType<EventTypesSetForWebhook>().Single().EventTypes.ShouldContainOnly(new EventType("customer", 3));
    [Fact] void should_not_change_the_delivery_policy() => _appended.OfType<GenerationDeliverySetForWebhook>().ShouldBeEmpty();
}
