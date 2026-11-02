// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class concurrently_with_a_conflicting_delivery_format : given.a_pinned_registration
{
    void Establish()
    {
        _definition.DeliveryFormat = Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookDeliveryFormat.Legacy;
        ALogWithLastLifecycleEvent(ALifecycleEvent<WebhookAdded>(5));
        TheStoredRegistrationHasFormat(WebhookDeliveryFormat.EnvelopeV1);
        TheScopedAppendFailsWithAConcurrencyViolation();
    }

    async Task Because() => _error = await Catch.Exception(() => _registrar.Add("store", [_definition]));

    [Fact] void should_refuse_the_format_change() => _error.ShouldBeOfExactType<WebhookDeliveryFormatCannotBeChanged>();
    [Fact] void should_not_append_anything() => AppendCount.ShouldEqual(0);
    [Fact] void should_not_append_an_authorization_event() => UnscopedAppendCount.ShouldEqual(0);
}
