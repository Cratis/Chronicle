// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Webhooks.for_WebhookDefinitionConverters.when_round_tripping;

public class with_envelope_delivery_format : Specification
{
    Concepts.Observation.Webhooks.WebhookDefinition _definition;
    Concepts.Observation.Webhooks.WebhookDefinition _result;

    void Establish() => _definition = new("webhook", WebhookOwner.Client, "log", [new EventType("customer", 3)], new("https://example.com/webhook", WebhookAuthorization.None, new Dictionary<string, string>()))
    {
        DeliveryFormat = WebhookDeliveryFormat.EnvelopeV1
    };

    void Because() => _result = _definition.ToMongoDB().ToKernel();

    [Fact] void should_preserve_the_delivery_format() => _result.DeliveryFormat.ShouldEqual(WebhookDeliveryFormat.EnvelopeV1);
}
