// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Webhooks.for_WebhookDefinitionConverters.when_round_tripping;

public class with_pinned_generations : Specification
{
    Concepts.Observation.Webhooks.WebhookDefinition _definition;
    Concepts.Observation.Webhooks.WebhookDefinition _result;

    void Establish() => _definition = new("webhook", WebhookOwner.Client, "log", [new EventType("customer", 3), new EventType("order", 2)], new("https://example.com/webhook", WebhookAuthorization.None, new Dictionary<string, string>()))
    {
        GenerationDelivery = EventGenerationDelivery.Pinned
    };

    void Because() => _result = _definition.ToSql().ToKernel();

    [Fact] void should_preserve_the_delivery_policy() => _result.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
    [Fact] void should_preserve_exact_pins() => _result.EventTypes.ShouldContainOnly(_definition.EventTypes);
}
