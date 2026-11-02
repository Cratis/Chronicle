// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookDefinitionDeliveryExtensions.when_checking_live_only;

public class with_the_envelope_format : Specification
{
    bool _result;

    void Because() => _result = new WebhookDefinition("webhook", WebhookOwner.Client, "log", [], new("https://example.com/webhook", WebhookAuthorization.None, new Dictionary<string, string>()))
    {
        DeliveryFormat = WebhookDeliveryFormat.EnvelopeV1
    }.IsLiveOnly();

    [Fact] void should_be_live_only() => _result.ShouldEqual(true);
}
