// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class a_valid_webhook_and_a_format_change_in_one_request : given.a_pinned_registration
{
    Contracts.Observation.Webhooks.WebhookDefinition _other;

    void Establish()
    {
        _other = new()
        {
            Identifier = "other",
            GenerationDelivery = Contracts.Observation.EventGenerationDelivery.Pinned,
            EventTypes = [new() { Id = "customer", Generation = 3 }],
            Target = new() { Url = "https://example.com/other" },
            DeliveryFormat = Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookDeliveryFormat.EnvelopeV1
        };
        _manager.GetWebhookDefinitions().Returns([_other.ToChronicle() with { DeliveryFormat = WebhookDeliveryFormat.Legacy }]);
    }

    async Task Because() => _error = await Catch.Exception(() => _registrar.Add("store", [_definition, _other]));

    [Fact] void should_refuse_the_format_change() => _error.ShouldBeOfExactType<WebhookDeliveryFormatCannotBeChanged>();
    [Fact] void should_not_append_anything_for_any_webhook() => AppendCount.ShouldEqual(0);
}
