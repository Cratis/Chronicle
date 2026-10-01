// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Observation;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_retiring;

public class and_failed_partition_reminders_exist : given.an_observer_with_subscription
{
    IGrainReminder _reminder;
    ObserverRunningState _runningState;

    async Task Establish()
    {
        _failedPartitionsState.AddFailedPartition("partition", 12UL);
        _reminder = await _observer.RegisterOrUpdateReminder("partition", TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        _runningState = (await _observer.GetState()).RunningState;
        _observerAlerts.ClearReceivedCalls();
    }

    async Task Because() => await _observer.Retire();

    [Fact] void should_unregister_the_retry_reminder() => _silo.ReminderRegistry.Mock.Verify(registry => registry.UnregisterReminder(It.IsAny<GrainId>(), _reminder), Times.Once);
    [Fact] void should_keep_the_running_state() => _stateStorage.State.RunningState.ShouldEqual(_runningState);
    [Fact] async Task should_discard_failed_partitions() => (await _observer.HasFailedPartitions()).ShouldBeFalse();
    [Fact] async Task should_clear_incidents_as_removed_only() => await _observerAlerts.Received(1).Removed();
    [Fact] async Task should_not_send_a_cleared_snapshot() => await _observerAlerts.DidNotReceive().Reconcile(Arg.Any<ObserverAlertSnapshot>());
}
