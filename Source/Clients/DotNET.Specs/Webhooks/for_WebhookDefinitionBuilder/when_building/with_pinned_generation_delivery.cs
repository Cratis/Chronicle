// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_WebhookDefinitionBuilder.when_building;

public class with_pinned_generation_delivery : Specification
{
    WebhookDefinitionBuilder _builder;
    WebhookDefinition _result;

    void Establish()
    {
        var eventTypes = Substitute.For<IEventTypes>();
        eventTypes.All.Returns([new EventType("customer", 3)]);
        _builder = new(eventTypes);
        _builder.WithGenerationDelivery(EventGenerationDelivery.Pinned);
    }

    void Because() => _result = _builder.Build("webhook", "https://example.com/webhook");

    [Fact] void should_opt_in() => _result.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
    [Fact] void should_send_the_policy_to_the_kernel() => _result.ToContract().GenerationDelivery.ShouldEqual(Contracts.Observation.EventGenerationDelivery.Pinned);
    [Fact] void should_preserve_the_generation() => _result.ToContract().EventTypes.Single().Generation.ShouldEqual(3u);
}
