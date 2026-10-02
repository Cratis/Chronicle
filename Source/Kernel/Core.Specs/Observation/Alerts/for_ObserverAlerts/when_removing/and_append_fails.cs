// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_removing;

public class and_append_fails : given.an_alert_tracker
{
    void Establish()
    {
        GivenHistory(RaisedForSnapshot());
        AppendReturns(AppendResult.Failed(CorrelationId.NotSet, (AppendError[])[new("Unavailable")]));
    }

    async Task Because() => _receipt = await ReconcileRemoval();

    [Fact] void should_require_a_retry_before_cleanup() => _receipt.Outcome.ShouldEqual(ObserverAlertReconciliation.RetryRequired);
    [Fact] void should_attempt_a_removed_clear() => ((AlertCleared)_serialized).Reason.ShouldEqual(AlertClearedReason.Removed);
    [Fact] void should_count_the_failed_append() => _metrics.SumOf("chronicle-alert-transitions-failed").ShouldEqual(1);
}
