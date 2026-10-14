// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Specs.Webhooks.for_Webhooks.when_listing;

public class with_pinned_generation_delivery : given.webhook_query_responses
{
    void Establish() => _response.GenerationDelivery = Contracts.Observation.EventGenerationDelivery.Pinned;
    async Task Because() => _definition = (await _webhooks.GetAll()).Single();

    [Fact] void should_report_the_pinned_policy() => _definition.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
    [Fact] void should_preserve_the_exact_pin() => _definition.EventTypes.Single().ShouldEqual(new EventType("person-registered", 2));
}
