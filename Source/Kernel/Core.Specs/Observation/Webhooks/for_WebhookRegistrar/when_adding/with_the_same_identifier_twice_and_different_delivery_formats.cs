// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class with_the_same_identifier_twice_and_different_delivery_formats : given.a_pinned_registration
{
    Contracts.Observation.Webhooks.WebhookDefinition _second;

    void Establish()
    {
        _second = new()
        {
            Identifier = _definition.Identifier,
            GenerationDelivery = _definition.GenerationDelivery,
            EventTypes = _definition.EventTypes,
            Target = _definition.Target,
            DeliveryFormat = Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookDeliveryFormat.EnvelopeV1
        };
    }

    async Task Because() => _error = await Catch.Exception(() => _registrar.Add("store", [_definition, _second]));

    [Fact] void should_refuse_the_request() => _error.ShouldBeOfExactType<ConflictingWebhookDeliveryFormatsInRequest>();
    [Fact] void should_not_append_anything() => AppendCount.ShouldEqual(0);
}
