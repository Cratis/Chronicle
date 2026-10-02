// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_removing;

public class and_source_deletion_commits_but_throws : given.an_observer
{
    Exception _error;

    void Establish() => _eventStoreNamespaceStorage.Observers.Delete(_observerId).Returns(_ =>
    {
        _eventStoreNamespaceStorage.Observers.Get(_observerId).Returns(ObserverState.Empty);
        return Task.FromException(new TimeoutException());
    });

    async Task Because()
    {
        _error = await Catch.Exception(_observer.Remove);
        _storageStats.ResetCounts();
        _observerHandledCountsStorage.ClearReceivedCalls();
        await _observer.ReportHandledEvents("partition", new Dictionary<EventTypeId, EventCount> { ["event"] = 1 });
        await _observer.FailedPartitionRecovered("partition", 42UL);
        await _observer.ReceiveReminder(Observer.AlertReminderName, default);
        await _observer.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.ApplicationRequested, "Spec"), default);
    }

    [Fact] void should_propagate_deletion_uncertainty() => _error.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_not_resurrect_source_state() => _storageStats.Writes.ShouldEqual(0);
    [Fact] void should_not_recreate_handled_counts() => _observerHandledCountsStorage.ReceivedCalls().ShouldBeEmpty();
    [Fact] async Task should_keep_the_reminder_until_storage_is_checked_on_reactivation() => (await _observer.GetReminder(Observer.AlertReminderName)).ShouldNotBeNull();
}
