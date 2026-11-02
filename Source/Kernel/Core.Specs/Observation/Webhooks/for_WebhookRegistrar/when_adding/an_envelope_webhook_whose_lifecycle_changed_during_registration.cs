// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class an_envelope_webhook_whose_lifecycle_changed_during_registration : given.a_pinned_registration
{
    void Establish()
    {
        _definition.DeliveryFormat = Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookDeliveryFormat.EnvelopeV1;
        TheScopedAppendFailsWithAConcurrencyViolation();
    }

    async Task Because() => _error = await Catch.Exception(() => _registrar.Add("store", [_definition]));

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<WebhookLifecycleChangedDuringRegistration>();
    [Fact] void should_not_read_the_lifecycle_again() => LifecycleReads.ShouldEqual(1);
    [Fact] void should_not_attempt_a_second_append() => AppendCount.ShouldEqual(1);
}
