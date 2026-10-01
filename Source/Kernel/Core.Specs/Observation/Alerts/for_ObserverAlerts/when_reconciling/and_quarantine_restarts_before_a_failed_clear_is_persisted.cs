// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_reconciling;

public class and_quarantine_restarts_before_a_failed_clear_is_persisted : given.an_alert_tracker
{
    AlertRaised _original;

    async Task Establish()
    {
        _original = new(IncidentId.New(), AlertConditionKind.ObserverQuarantined, AlertSeverity.Critical, AlertTarget.For(_key, AlertPartition.None), AlertEvidence.Create(0, _clock.Now, _clock.Now, FailureKind.Unknown, string.Empty));
        GivenHistory(_original);
        _snapshot = _snapshot with { FailedPartitions = [], Endings = new Dictionary<IncidentId, AlertClearedReason> { [_original.IncidentId] = AlertClearedReason.Revived } };
        AppendReturns(AppendResult.Failed(CorrelationId.NotSet, (AppendError[])[new("Unavailable")]));
        await _tracker.Reconcile(_snapshot);
        GivenHistory(_original);
        AppendSucceedsFrom(1);
    }

    async Task Because()
    {
        _snapshot = _snapshot with { IsQuarantined = true, QuarantineEpisodeId = Guid.NewGuid(), Revision = 2 };
        await _tracker.Reconcile(_snapshot);
    }

    [Fact] void should_complete_the_previous_episode_first() => _appends[0].ShouldBeOfExactType<AlertCleared>();
    [Fact] void should_clear_the_original_incident() => ((AlertCleared)_appends[0]).IncidentId.ShouldEqual(_original.IncidentId);
    [Fact] void should_preserve_the_revive_reason() => ((AlertCleared)_appends[0]).Reason.ShouldEqual(AlertClearedReason.Revived);
    [Fact] void should_raise_a_new_quarantine_episode() => _appends[1].ShouldBeOfExactType<AlertRaised>();
    [Fact] void should_not_reuse_the_original_incident() => (((AlertRaised)_appends[1]).IncidentId == _original.IncidentId).ShouldBeFalse();
    [Fact] void should_append_only_the_clear_and_new_raise() => _appends.Count.ShouldEqual(2);
}
