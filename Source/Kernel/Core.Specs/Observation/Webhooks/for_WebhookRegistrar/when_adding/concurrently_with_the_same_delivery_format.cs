// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class concurrently_with_the_same_delivery_format : given.a_pinned_registration
{
    void Establish()
    {
        ALogWithLastLifecycleEvent(Cratis.Monads.Option<AppendedEvent>.None(), ALifecycleEvent<WebhookAdded>(5));
        TheStoredRegistrationHasFormat(WebhookDeliveryFormat.Legacy);
        TheScopedAppendFailsWithAConcurrencyViolation();
    }

    async Task Because() => _error = await Catch.Exception(() => _registrar.Add("store", [_definition]));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_attempt_exactly_one_webhook_added_append() => ScopedAppends.Count().ShouldEqual(1);
    [Fact] void should_not_append_an_authorization_event() => UnscopedAppendCount.ShouldEqual(0);
}
