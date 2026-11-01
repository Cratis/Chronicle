// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_ownership_moves;

public class and_late_work_arrives_after_relinquishing : given.a_live_observer_with_guarded_state
{
    IGrainReminder _retryReminder;
    IGrainReminder _alertReminder;
    ObserverState _before;

    protected override Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 600 };

    async Task Establish()
    {
        await _observer.PartitionFailed("partition", 42UL, ["failure"], "stack trace");
        await _observer.RunWatchdogAsync();
        _retryReminder = await _observer.GetReminder(Observer.PartitionReminderName(_failedPartitionsState.Partitions.Single().Id));
        _alertReminder = await _observer.GetReminder(Observer.AlertReminderName);
        await _observer.ReportHandledEvents("pending", new Dictionary<EventTypeId, EventCount> { ["type"] = 2UL }, JobStepId.New(), 0UL);
        _observerHandledCountsStorage.ClearReceivedCalls();
        _eventStoreNamespaceStorage.FailedPartitions.ClearReceivedCalls();
        _jobsManager.ClearReceivedCalls();
    }

    async Task Because()
    {
        await MoveDefinition();
        _before = await _guarded.Get(_observerId);
        await _observer.RunWatchdogAsync();
        await _observer.FailedPartitionRecovered("partition", 42UL);
        await _observer.ReportHandledEvents("pending", new Dictionary<EventTypeId, EventCount> { ["type"] = 5UL }, JobStepId.New(), 1UL);
        await _observer.PartitionFailed("late-failure", 43UL, ["late failure"], "stack trace");
        await _observer.RunWatchdogAsync();
        await _observer.RunWatchdogAsync();
        await _observer.SetHandledStats(999);
    }

    [Fact] async Task should_keep_the_new_owners_state_unchanged() => (await _guarded.Get(_observerId)).ShouldEqual(_before);
    [Fact] void should_drop_pending_and_late_handled_counts() => _observerHandledCountsStorage.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_start_or_resume_jobs() => _jobsManager.ReceivedCalls().ShouldBeEmpty();
    [Fact] async Task should_not_persist_a_late_failure() => await _eventStoreNamespaceStorage.FailedPartitions.DidNotReceive().Save(Arg.Any<ObserverId>(), Arg.Any<FailedPartitions>());
    [Fact] void should_unregister_the_retry_reminder_once() => _silo.ReminderRegistry.Mock.Verify(registry => registry.UnregisterReminder(It.IsAny<GrainId>(), _retryReminder), Times.Once);
    [Fact] void should_unregister_the_alert_reminder_once() => _silo.ReminderRegistry.Mock.Verify(registry => registry.UnregisterReminder(It.IsAny<GrainId>(), _alertReminder), Times.Once);
    [Fact] async Task should_stay_unsubscribed() => (await _observer.IsSubscribed()).ShouldBeFalse();
}
