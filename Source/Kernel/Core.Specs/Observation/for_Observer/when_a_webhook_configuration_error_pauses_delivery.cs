// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_webhook_configuration_error_pauses_delivery : given.a_webhook_observer
{
    async Task Because() => await _observer.PartitionFailed("partition", 42UL, ["403"], "", FailureKind.Handling, failurePolicy: WebhookDeliveryFailurePolicy.PauseObserver);

    [Fact] void should_pause_the_whole_observer() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_preserve_one_failure() => _failedPartitionsState.Partitions.Single().Attempts.Count().ShouldEqual(1);
    [Fact] void should_not_schedule_a_timed_retry() => _failedPartitionsState.Partitions.Single().Deferral.ShouldBeNull();
    [Fact] void should_unregister_from_live_delivery() => _appendedEventsQueues.Received().Unsubscribe(Arg.Is<EventSequences.AppendedEventsQueueSubscription>(subscription => subscription.ObserverKey == _observerKey));
}
