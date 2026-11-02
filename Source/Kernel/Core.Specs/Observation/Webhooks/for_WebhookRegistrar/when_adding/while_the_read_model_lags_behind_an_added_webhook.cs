// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class while_the_read_model_lags_behind_an_added_webhook : given.a_pinned_registration
{
    void Establish()
    {
        ALogWithLastLifecycleEvent(ALifecycleEvent<WebhookAdded>(5));
        TheStoredRegistrationHasFormat(WebhookDeliveryFormat.EnvelopeV1);
    }

    async Task Because() => _error = await Catch.Exception(() => _registrar.Add("store", [_definition]));

    [Fact] void should_refuse_the_format_change() => _error.ShouldBeOfExactType<WebhookDeliveryFormatCannotBeChanged>();
    [Fact] void should_not_append() => AppendCount.ShouldEqual(0);
}
