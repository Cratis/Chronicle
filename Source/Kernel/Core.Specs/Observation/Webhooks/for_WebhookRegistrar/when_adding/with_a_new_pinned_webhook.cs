// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class with_a_new_pinned_webhook : given.a_pinned_registration
{
    async Task Because() => await _registrar.Add("store", [_definition]);
    [Fact] void should_record_the_opt_in() => _appended.OfType<WebhookAdded>().Single().GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
    [Fact] void should_record_the_exact_generation() => _appended.OfType<WebhookAdded>().Single().EventTypes.ShouldContainOnly(new EventType("customer", 3));
}
