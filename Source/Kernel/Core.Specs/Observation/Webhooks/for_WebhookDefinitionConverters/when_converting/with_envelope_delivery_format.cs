// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookDefinitionConverters.when_converting;

public class with_envelope_delivery_format : Specification
{
    Contracts.Observation.Webhooks.WebhookDefinition _contract;
    WebhookDefinition _definition;
    Contracts.Observation.Webhooks.WebhookDefinition _roundTripped;
    WebhookDetails _readModel;
    WebhookDefinition _legacy;

    void Establish() => _contract = new()
    {
        Identifier = "webhook",
        Target = new() { Url = "https://example.com/webhook" },
        DeliveryFormat = Contracts.Observation.Webhooks.WebhookDeliveryFormat.EnvelopeV1
    };

    void Because()
    {
        _definition = _contract.ToChronicle();
        _roundTripped = _definition.ToContract();
        _readModel = _definition.ToReadModel();
        _legacy = new Contracts.Observation.Webhooks.WebhookDefinition
        {
            Identifier = "legacy-webhook",
            Target = new() { Url = "https://example.com/webhook" }
        }.ToChronicle();
    }

    [Fact] void should_keep_the_format_in_the_definition() => _definition.DeliveryFormat.ShouldEqual(WebhookDeliveryFormat.EnvelopeV1);
    [Fact] void should_keep_the_format_in_the_contract() => _roundTripped.DeliveryFormat.ShouldEqual(Contracts.Observation.Webhooks.WebhookDeliveryFormat.EnvelopeV1);
    [Fact] void should_keep_the_format_in_the_read_model() => _readModel.DeliveryFormat.ShouldEqual(WebhookDeliveryFormat.EnvelopeV1);
    [Fact] void should_default_an_old_contract_to_legacy() => _legacy.DeliveryFormat.ShouldEqual(WebhookDeliveryFormat.Legacy);
}
