// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_paused_webhook_needs_a_probe : given.a_webhook_observer
{
    Task Because() => _observer.PartitionFailed("partition", 42UL, ["403"], "", FailureKind.Handling, failurePolicy: WebhookDeliveryFailurePolicy.PauseObserver);

    [Fact] void should_arm_a_durable_probe() => _silo.ReminderRegistry.Mock.Verify(registry => registry.RegisterOrUpdateReminder(It.IsAny<GrainId>(), "webhook-quarantine-probe", It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()), Times.AtLeastOnce());
    [Fact] void should_remain_quarantined() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_keep_the_real_attempt() => _failedPartitionsState.Partitions.Single().Attempts.Count().ShouldEqual(1);
}
