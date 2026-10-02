// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_reconciling;

public class and_the_append_drain_times_out : given.a_tracker_with_an_unfinished_append
{
    ObserverAlertReceipt _beforeCommit;

    async Task Establish()
    {
        _snapshot = _snapshot with { FailedPartitions = [], Revision = _snapshot.Revision + 1 };
        _sequence.Configure().DrainAppends().Returns(Task.FromException(new TimeoutException()));
        _beforeCommit = await _tracker.Reconcile(_snapshot);
        CompleteAppend();
        _sequence.Configure().DrainAppends().Returns(Task.CompletedTask);
    }

    async Task Because() => _receipt = await _tracker.Reconcile(_snapshot);

    [Fact] void should_keep_the_healthy_observer_reporting_until_the_drain_succeeds() => _beforeCommit.Outcome.ShouldEqual(ObserverAlertReconciliation.RetryRequired);
    [Fact] void should_clear_the_late_raise_as_recovered() => _appends.OfType<AlertCleared>().Single().Reason.ShouldEqual(AlertClearedReason.Recovered);
    [Fact] void should_acknowledge_only_after_the_clear_is_durable() => _receipt.Outcome.ShouldEqual(ObserverAlertReconciliation.Applied);
}
