// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_reconciling;

public class and_reactivation_precedes_a_timed_out_append_commit : given.a_tracker_with_an_unfinished_append
{
    async Task Establish()
    {
        _snapshot = _snapshot with { FailedPartitions = [], Revision = _snapshot.Revision + 1 };
        await CrashTracker();
    }

    Task Because() => ReconcileBeforeLateCommit();

    [Fact] void should_not_acknowledge_recovery_before_the_previous_activations_append_finishes() => _acknowledgedBeforeCommit.ShouldBeFalse();
    [Fact] void should_clear_the_late_raise_as_recovered() => _appends.OfType<AlertCleared>().Single().Reason.ShouldEqual(AlertClearedReason.Recovered);
    [Fact] void should_acknowledge_only_after_the_clear_is_durable() => _receipt.Outcome.ShouldEqual(ObserverAlertReconciliation.Applied);
}
