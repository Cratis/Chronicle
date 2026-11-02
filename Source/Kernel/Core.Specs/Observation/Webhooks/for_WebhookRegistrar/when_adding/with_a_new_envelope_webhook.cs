// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class with_a_new_envelope_webhook : given.a_pinned_registration
{
    void Establish() => _definition.DeliveryFormat = Contracts.Observation.Webhooks.WebhookDeliveryFormat.EnvelopeV1;
    async Task Because() => await _registrar.Add("store", [_definition]);
    [Fact] void should_record_the_delivery_format() => _appended.OfType<WebhookAdded>().Single().DeliveryFormat.ShouldEqual(WebhookDeliveryFormat.EnvelopeV1);
}
