// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Alerts;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentFold.when_folding_history;

public class and_the_open_projection_matches_observer_alert_evaluation : Specification
{
    Dictionary<IncidentId, OpenIncident> _expected = [];
    Dictionary<IncidentId, AlertIncident> _actual = [];
    object[] _history;

    void Establish()
    {
        var first = new IncidentId(Guid.NewGuid());
        var orphan = new IncidentId(Guid.NewGuid());
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var target = new AlertTarget("store", "tenant", "observer", EventSequenceId.Log, "[none]");
        var evidence = new AlertEvidence(3, now, now, FailureKind.Handling, "recorded");
        _history = [
            new AlertEscalated(orphan, AlertConditionKind.PartitionFailing, AlertSeverity.Critical, target, evidence),
            new AlertRaised(first, AlertConditionKind.PartitionFailing, AlertSeverity.Warning, target, evidence),
            new AlertEscalated(first, "future-condition", AlertSeverity.Critical, target with { Partition = "changed" }, evidence),
            new AlertCleared(first, "future-condition", AlertClearedReason.Recovered, target),
            new AlertCleared(first, "future-condition", AlertClearedReason.Cleared, target),
            new AlertEscalated(first, "future-condition", AlertSeverity.Critical, target, evidence),
            new AlertRaised(first, AlertConditionKind.PartitionRetriesExhausted, AlertSeverity.Critical, target, evidence)];
    }

    void Because()
    {
        ulong sequence = 0;
        foreach (var recorded in _history)
        {
            ObserverAlertEvaluation.Apply(recorded, _expected);
            var context = EventContext.Empty with { SequenceNumber = sequence++, Occurred = DateTimeOffset.UnixEpoch };
            var transition = recorded switch
            {
                AlertRaised raised => raised.ToTransition(context),
                AlertEscalated escalated => escalated.ToTransition(context),
                AlertCleared cleared => cleared.ToTransition(context),
                _ => throw new InvalidAlertIncidentQuery("Unsupported test event.")
            };
            _actual.TryGetValue(transition.Id, out var current);
            var result = AlertIncidentFold.Apply(current, transition);
            if (result.Incident is not null) _actual[transition.Id] = result.Incident;
        }
    }

    [Fact]
    void should_match_the_authoritative_open_projection() => _actual.Values.Where(row => row.IsOpen)
        .Select(row => new OpenIncident(row.Id, row.Condition, row.Severity!.Value, row.Target.Partition)).ShouldContainOnly(_expected.Values.ToArray());
}
