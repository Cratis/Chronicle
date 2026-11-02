// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class with_an_existing_legacy_webhook_and_the_envelope_format : given.a_pinned_registration
{
    void Establish()
    {
        ExistingWebhookWithFormat(WebhookDeliveryFormat.Legacy);
        _definition.DeliveryFormat = Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookDeliveryFormat.EnvelopeV1;
    }

    async Task Because() => _error = await Catch.Exception(() => _registrar.Add("store", [_definition]));

    [Fact] void should_refuse_the_format_change() => _error.ShouldBeOfExactType<WebhookDeliveryFormatNotSupported>();
    [Fact] void should_not_append() => AppendCount.ShouldEqual(0);
    [Fact] void should_not_compare_definitions() => _webhookDefinitionComparer.DidNotReceiveWithAnyArgs().Compare(default!, default!, default!);
}
