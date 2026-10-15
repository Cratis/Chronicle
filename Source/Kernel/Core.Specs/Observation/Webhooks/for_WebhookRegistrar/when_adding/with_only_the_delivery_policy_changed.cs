// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class with_only_the_delivery_policy_changed : given.a_pinned_registration
{
    void Establish() => ExistingDefinition(false, true);
    async Task Because() => await _registrar.Add("store", [_definition]);
    [Fact] void should_record_the_policy_change() => _appended.OfType<GenerationDeliverySetForWebhook>().Single().GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
    [Fact] void should_record_the_pins_with_the_policy() => _appended.OfType<GenerationDeliverySetForWebhook>().Single().EventTypes.ShouldContainOnly(new EventType("customer", 3));
    [Fact] void should_append_only_one_change() => _appended.Count.ShouldEqual(1);
}
