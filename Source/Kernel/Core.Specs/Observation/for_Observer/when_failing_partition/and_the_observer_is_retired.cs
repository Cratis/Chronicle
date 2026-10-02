// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

public class and_the_observer_is_retired : given.a_reactivated_quarantined_observer
{
    ObserverState _state;

    async Task Establish()
    {
        await _observer.Retire();
        _state = await _observer.GetState();
        _storageStats.ResetCounts();
        _failedPartitionsStorageStats.ResetCounts();
        _observerAlerts.ClearReceivedCalls();
    }

    async Task Because() => await _observer.PartitionFailed("partition", 42UL, ["Late replay failure"], "Stack");

    [Fact] void should_not_record_a_failure() => _failedPartitionsState.HasFailedPartitions.ShouldBeFalse();
    [Fact] async Task should_not_change_observer_state() => (await _observer.GetState()).ShouldEqual(_state);
    [Fact] void should_not_persist_observer_state() => _storageStats.Writes.ShouldEqual(0);
    [Fact] void should_not_persist_failures() => _failedPartitionsStorageStats.Writes.ShouldEqual(0);
    [Fact] async Task should_not_report_alerts() => await _observerAlerts.DidNotReceive().Reconcile(Arg.Any<ObserverAlertSnapshot>());
}
