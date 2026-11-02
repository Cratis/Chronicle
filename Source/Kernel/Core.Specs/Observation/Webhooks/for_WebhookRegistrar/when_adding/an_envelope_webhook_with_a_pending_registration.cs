// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class an_envelope_webhook_with_a_pending_registration : given.a_pinned_registration
{
    void Establish()
    {
        _definition.DeliveryFormat = Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookDeliveryFormat.EnvelopeV1;
        ALogWithLastLifecycleEvent(ALifecycleEvent<WebhookAdded>(3));
    }

    async Task Because() => _error = await Catch.Exception(() => _registrar.Add("store", [_definition]));

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<WebhookLifecycleChangedDuringRegistration>();
    [Fact] void should_not_append() => AppendCount.ShouldEqual(0);
}
