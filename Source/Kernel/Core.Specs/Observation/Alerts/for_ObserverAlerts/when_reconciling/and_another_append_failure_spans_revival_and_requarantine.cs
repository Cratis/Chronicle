// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_reconciling;

public class and_another_append_failure_spans_revival_and_requarantine : given.an_alert_tracker
{
    Guid _original;
    Guid _current;

    async Task Establish()
    {
        _original = Guid.NewGuid();
        _current = Guid.NewGuid();
        GivenHistory(new AlertRaised(new(_original), AlertConditionKind.ObserverQuarantined, AlertSeverity.Critical, AlertTarget.For(_key, AlertPartition.None), RaisedForSnapshot().Evidence));
        _snapshot = _snapshot with { IsQuarantined = true, QuarantineEpisodeId = _original };
        AppendReturns(AppendResult.Failed(CorrelationId.NotSet, (AppendError[])[new("Partition raise unavailable")]));
        await _tracker.Reconcile(_snapshot);
        _snapshot = _snapshot with { IsQuarantined = false, QuarantineEpisodeId = null, Revision = 2 };
        await _tracker.Reconcile(_snapshot);
        _snapshot = _snapshot with { IsQuarantined = true, QuarantineEpisodeId = _current, Revision = 3, FailedPartitions = [] };
        AppendSucceedsFrom(1);
    }

    async Task Because() => _receipt = await _tracker.Reconcile(_snapshot);

    [Fact] void should_clear_the_original_episode_first() => ((AlertCleared)_appends[0]).IncidentId.Value.ShouldEqual(_original);
    [Fact] void should_raise_the_current_episode() => ((AlertRaised)_appends[1]).IncidentId.Value.ShouldEqual(_current);
    [Fact] void should_omit_the_unobserved_partition_episode() => _appends.Count.ShouldEqual(2);
    [Fact] void should_acknowledge_current_application() => _receipt.Outcome.ShouldEqual(ObserverAlertReconciliation.Applied);
}
