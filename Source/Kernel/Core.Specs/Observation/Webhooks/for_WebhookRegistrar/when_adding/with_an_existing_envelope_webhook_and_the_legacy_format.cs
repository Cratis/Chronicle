// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class with_an_existing_envelope_webhook_and_the_legacy_format : given.a_pinned_registration
{
    void Establish() => ExistingWebhookWithFormat(WebhookDeliveryFormat.EnvelopeV1);

    async Task Because() => _error = await Catch.Exception(() => _registrar.Add("store", [_definition]));

    [Fact] void should_refuse_the_format_change() => _error.ShouldBeOfExactType<WebhookDeliveryFormatCannotBeChanged>();
    [Fact] void should_not_append() => AppendCount.ShouldEqual(0);
    [Fact] void should_not_compare_definitions() => _webhookDefinitionComparer.DidNotReceiveWithAnyArgs().Compare(default!, default!, default!);
}
