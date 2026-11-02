// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookAdmission.when_writing;

public class with_an_existing_identical_registration : given.an_envelope_write
{
    void Establish()
    {
        ALogWithLastLifecycleEvent(ALifecycleEvent<WebhookAdded>(3));
        ExistingWebhookWithFormat(WebhookDeliveryFormat.EnvelopeV1);
        _events = [];
        _authorization = WebhookAuthorization.None;
    }

    async Task Because() => _error = await Catch.Exception(Write);

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_build_changes() => _comparisons.ShouldEqual(1);
    [Fact] void should_not_append_for_same() => AppendCount.ShouldEqual(0);
}
