// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_webhook_reaches_five_parked_partitions : given.a_webhook_observer
{
    protected override Configuration.Observers CreateWebhookRetryConfig() => new Configuration.Webhooks().Retry;

    void Establish()
    {
        for (var index = 0; index < 6; index++)
        {
            _failedPartitionsState.RegisterAttempt($"resolved-{index}", 42UL, ["503"], "", FailureKind.Handling);
            _failedPartitionsState.Remove($"resolved-{index}");
        }
        for (var index = 0; index < 4; index++)
        {
            _failedPartitionsState.RegisterAttempt($"parked-{index}", 42UL, ["410"], "", FailureKind.Handling);
            _failedPartitionsState.Quarantine($"parked-{index}");
        }
    }

    async Task Because() => await _observer.PartitionFailed("partition", 42UL, ["410"], "", FailureKind.Handling, failurePolicy: WebhookDeliveryFailurePolicy.ParkPartition);

    [Fact] void should_pause_the_observer_at_the_default_threshold() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
}
