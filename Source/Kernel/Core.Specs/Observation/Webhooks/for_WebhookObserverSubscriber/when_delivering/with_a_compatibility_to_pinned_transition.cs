// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookObserverSubscriber.when_delivering;

public class with_a_compatibility_to_pinned_transition : given.a_compatibility_subscription_with_a_pinned_definition
{
    readonly TaskCompletionSource _response = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _acknowledgedBeforeHttpSuccess;

    void Establish() => _receiver.ResponseGate = _response.Task;

    async Task Because()
    {
        var delivery = _subscriber.OnNext(_partition, _events, new(null));
        await _receiver.RequestReceived.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _acknowledgedBeforeHttpSuccess = delivery.IsCompleted;
        _response.SetResult();
        _deliveryResult = await delivery;
    }

    [Fact] void should_release_the_current_pin_content_schema_hash_and_appended_generation() => _receiver.Payloads.Single().ShouldEqual(Fixture("pinned"));
    [Fact] void should_not_acknowledge_before_http_success() => _acknowledgedBeforeHttpSuccess.ShouldBeFalse();
    [Fact] void should_acknowledge_the_delivered_batch() => _deliveryResult.LastSuccessfulObservation.Value.ShouldEqual(44ul);
}
