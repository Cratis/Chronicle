// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Observation.for_Observer.given;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_the_observer_was_retired : an_observer_with_subscription
{
    async Task Establish()
    {
        _failedPartitionsState.AddFailedPartition("partition", 12UL);
        await _observer.TransitionTo<QuarantinedObserver>();

        await _observer.Retire();
        _observerAlerts.ClearReceivedCalls();
    }

    async Task Because() => await _observer.RunWatchdogAsync();

    [Fact] async Task should_have_no_failed_partitions_in_memory() => (await _observer.HasFailedPartitions()).ShouldBeFalse();
    [Fact] void should_have_no_failed_partitions_in_storage() => _failedPartitionsStorage.State.HasFailedPartitions.ShouldBeFalse();
    [Fact] async Task should_keep_the_quarantined_state() => (await _observer.IsObserverQuarantined()).ShouldBeTrue();
    [Fact] async Task should_remain_unsubscribed() => (await _observer.IsSubscribed()).ShouldBeFalse();
    [Fact] async Task should_not_send_a_snapshot_that_could_raise_retired_incidents_again() => await _observerAlerts.DidNotReceive().Reconcile(Arg.Any<ObserverAlertSnapshot>());
}
