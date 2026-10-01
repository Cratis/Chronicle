// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_reconciling;

public class and_a_clear_append_is_retried : given.an_alert_tracker
{
    async Task Establish()
    {
        GivenHistory(RaisedForSnapshot());
        _snapshot = _snapshot with { FailedPartitions = [], PartitionsEndedAs = AlertClearedReason.Cleared };
        AppendReturns(AppendResult.Failed(CorrelationId.NotSet, (AppendError[])[new("Unavailable")]));
        await _tracker.Reconcile(_snapshot);

        // Re-reading on the next reconciliation still sees the raise, because the clear was not durable.
        _cursor.MoveNext().Returns(true, false);
        AppendReturns(AppendResult.Success(CorrelationId.NotSet, 1UL));
    }

    async Task Because() => await _tracker.Reconcile(_snapshot with { PartitionsEndedAs = AlertClearedReason.Recovered });

    [Fact] void should_keep_the_original_operator_clear_reason() => ((AlertCleared)_serialized).Reason.ShouldEqual(AlertClearedReason.Cleared);
    [Fact] void should_count_only_the_failed_attempt() => _metrics.SumOf("chronicle-alert-transitions-failed").ShouldEqual(1);
}
