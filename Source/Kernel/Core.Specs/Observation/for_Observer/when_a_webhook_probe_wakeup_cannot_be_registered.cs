// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_webhook_probe_wakeup_cannot_be_registered : given.a_webhook_observer
{
    void Establish() => _silo.ReminderRegistry.Mock.Setup(registry => registry.RegisterOrUpdateReminder(It.IsAny<GrainId>(), "webhook-quarantine-probe", It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>())).ThrowsAsync(new InvalidOperationException("Reminder storage unavailable"));

    Task Because() => _observer.PartitionFailed("partition", 42UL, ["403"], "", FailureKind.Handling, failurePolicy: WebhookDeliveryFailurePolicy.PauseObserver);

    [Fact] void should_preserve_durable_eligibility_for_later_reconciliation() => _stateStorage.State.WebhookProbe.ShouldNotBeNull();
    [Fact] void should_keep_the_observer_quarantined() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_keep_the_actual_failure_history() => _failedPartitionsState.Partitions.Single().Attempts.Count().ShouldEqual(1);
}
