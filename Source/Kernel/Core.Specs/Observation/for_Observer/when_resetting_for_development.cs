// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_resetting_for_development : given.an_observer_debouncing_progress_persistence
{
    async Task Establish() => await _observer.SetHandledStats(42UL);

    async Task Because()
    {
        await _observer.ResetForDevelopment();
        _storageStats.ResetCounts();
        _failedPartitionsStorageStats.ResetCounts();
        _jobsManager.ClearReceivedCalls();
        await _observer.CatchUp();
        await _observer.CaughtUp(JobId.New(), 43UL);
        await _observer.PartitionFailed("late", 44UL, ["Late failure"], "Stack");
        await _observer.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.ApplicationRequested, "Development reset"), CancellationToken.None);
    }

    [Fact] async Task should_unsubscribe_from_the_live_queue() => await _appendedEventsQueues.Received().Unsubscribe(Arg.Any<AppendedEventsQueueSubscription>());
    [Fact] void should_not_repersist_the_old_cursor_after_reset() => _storageStats.Writes.ShouldEqual(0);
    [Fact] void should_not_repersist_late_failures() => _failedPartitionsStorageStats.Writes.ShouldEqual(0);
    [Fact] void should_not_start_jobs_from_late_callbacks() => _jobsManager.ReceivedCalls().ShouldBeEmpty();
}
