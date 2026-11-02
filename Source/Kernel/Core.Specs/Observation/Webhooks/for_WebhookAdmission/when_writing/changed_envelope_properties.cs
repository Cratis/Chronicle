// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookAdmission.when_writing;

public class changed_envelope_properties : given.an_envelope_write
{
    void Establish()
    {
        ALogWithLastLifecycleEvent(ALifecycleEvent<WebhookAdded>(3));
        ExistingWebhookWithFormat(WebhookDeliveryFormat.EnvelopeV1);
        _events = [new EventTypesSetForWebhook(_webhook.EventTypes), new TargetUrlSetForWebhook(_webhook.Target.Url), new TargetHeadersSetForWebhook(_webhook.Target.Headers)];
        _authorization = new BearerTokenAuthorization("token");
    }

    async Task Because() => _error = await Catch.Exception(Write);

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_fence_every_property_and_authorization_append() => ScopedAppends.Select(_ => _.SequenceNumber).ShouldEqual(Enumerable.Repeat((EventSequenceNumber)3, 4));
    [Fact] void should_never_append_unfenced() => UnscopedAppendCount.ShouldEqual(0);
}
