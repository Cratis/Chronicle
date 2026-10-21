// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookObserverSubscriber.when_retrying;

public class after_a_transition_release_failure : given.a_compatibility_subscription_with_a_pinned_definition
{
    ObserverSubscriberResult _failedResult;
    int _httpCallsAfterFailure;

    async Task Because()
    {
        var registeredDefinition = _definition;
        _definition = _definition with { Generations = _definition.Generations.Where(_ => _.Generation != _pin.Generation).ToArray() };
        _failedResult = await _subscriber.OnNext(_partition, _events, new(null));
        _httpCallsAfterFailure = _receiver.Payloads.Count;
        _definition = registeredDefinition;
        _deliveryResult = await _subscriber.OnNext(_partition, _events, new(null));
    }

    [Fact] void should_fail_the_first_attempt() => _failedResult.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_not_acknowledge_the_first_attempt() => _failedResult.LastSuccessfulObservation.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_not_send_until_release_succeeds() => _httpCallsAfterFailure.ShouldEqual(0);
    [Fact] void should_release_the_current_pin_on_retry() => _receiver.Payloads.Single().ShouldEqual(Fixture("pinned"));
    [Fact] void should_acknowledge_the_successful_retry() => _deliveryResult.LastSuccessfulObservation.Value.ShouldEqual(44ul);
}
