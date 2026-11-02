// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookAdmission.when_writing;

public class a_changed_property_with_a_lifecycle_race : given.an_envelope_write
{
    void Establish()
    {
        ALogWithLastLifecycleEvent(ALifecycleEvent<WebhookAdded>(3));
        ExistingWebhookWithFormat(WebhookDeliveryFormat.EnvelopeV1);
        _events = [new TargetUrlSetForWebhook(_webhook.Target.Url), new TargetHeadersSetForWebhook(_webhook.Target.Headers)];
        _authorization = new BasicAuthorization("user", "password");
        TheScopedAppendFailsWithAConcurrencyViolation();
    }

    async Task Because() => _error = await Catch.Exception(Write);

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<WebhookLifecycleChangedDuringRegistration>();
    [Fact] void should_not_append_more_properties_or_authorization() => AppendCount.ShouldEqual(1);
    [Fact] void should_not_retry_the_read() => LifecycleReads.ShouldEqual(1);
}
