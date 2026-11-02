// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class with_an_existing_envelope_webhook_unchanged : given.a_pinned_registration
{
    void Establish()
    {
        _definition.DeliveryFormat = Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookDeliveryFormat.EnvelopeV1;
        ExistingWebhookWithFormat(WebhookDeliveryFormat.EnvelopeV1);
        _webhookDefinitionComparer.Compare(Arg.Any<WebhookKey>(), Arg.Any<WebhookDefinition>(), Arg.Any<WebhookDefinition>())
            .Returns(new WebhookDefinitionComparisonResult(WebhookDefinitionCompareResult.Same, null));
    }

    async Task Because() => _error = await Catch.Exception(() => _registrar.Add("store", [_definition]));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_not_append() => AppendCount.ShouldEqual(0);
}
