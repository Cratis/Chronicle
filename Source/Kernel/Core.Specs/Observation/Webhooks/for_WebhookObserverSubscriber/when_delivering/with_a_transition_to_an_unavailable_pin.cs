// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookObserverSubscriber.when_delivering;

public class with_a_transition_to_an_unavailable_pin : given.a_compatibility_subscription_with_a_pinned_definition
{
    void Establish() => _definition = _definition with { Generations = _definition.Generations.Where(_ => _.Generation != _pin.Generation).ToArray() };

    async Task Because() => _deliveryResult = await _subscriber.OnNext(_partition, _events, new(null));

    [Fact] void should_fail_the_batch() => _deliveryResult.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_not_call_the_http_endpoint() => _receiver.Payloads.ShouldBeEmpty();
    [Fact] void should_not_acknowledge_any_events() => _deliveryResult.LastSuccessfulObservation.ShouldEqual(EventSequenceNumber.Unavailable);
}
