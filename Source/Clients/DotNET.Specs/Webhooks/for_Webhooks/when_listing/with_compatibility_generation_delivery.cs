// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Specs.Webhooks.for_Webhooks.when_listing;

public class with_compatibility_generation_delivery : given.webhook_query_responses
{
    async Task Because() => _definition = (await _webhooks.GetAll()).Single();

    [Fact] void should_keep_the_default_compatibility_policy() => _definition.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Compatibility);
}
