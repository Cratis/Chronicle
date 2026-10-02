// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_reconciling;

public class and_only_some_transitions_are_appended : given.an_alert_tracker
{
    ObserverAlertReceipt _partial;

    async Task Establish()
    {
        _snapshot = _snapshot with { FailedPartitions = [_snapshot.FailedPartitions.Single(), _snapshot.FailedPartitions.Single() with { Id = FailedPartitionId.New(), Partition = "second" }] };
        AppendUsing(() =>
        {
            if (_history.Count == 1) return AppendResult.Failed(CorrelationId.NotSet, (AppendError[])[new("Unavailable")]);
            RecordDurable(_serialized);
            return AppendResult.Success(CorrelationId.NotSet, 0UL);
        });
        _partial = await _tracker.Reconcile(_snapshot);
        await CrashTracker();
        AppendSucceedsFrom(1);
    }

    async Task Because() => _receipt = await _tracker.Reconcile(_snapshot);

    [Fact] void should_not_acknowledge_partial_application() => _partial.Outcome.ShouldEqual(ObserverAlertReconciliation.RetryRequired);
    [Fact] void should_append_only_the_remaining_raise() => _appends.OfType<AlertRaised>().Count().ShouldEqual(1);
    [Fact] void should_have_both_durable_raises() => _history.Count.ShouldEqual(2);
    [Fact] void should_acknowledge_the_completed_level() => _receipt.Outcome.ShouldEqual(ObserverAlertReconciliation.Applied);
}
