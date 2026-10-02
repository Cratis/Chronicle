// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Alerts.for_AlertIncidentFold.when_applying;

public class and_an_escalation_targets_a_closed_incident : given.a_recorded_transition
{
    void Establish()
    {
        _current = AlertIncidentFold.Apply(null, _raise with { Kind = AlertIncidentTransitionKind.Cleared }).Incident;
        _transition = _raise with { Kind = AlertIncidentTransitionKind.Escalated, SequenceNumber = 11UL };
    }

    void Because() => _result = AlertIncidentFold.Apply(_current, _transition);

    [Fact] void should_ignore_the_orphan() => _result.Outcome.ShouldEqual(AlertIncidentWriteOutcome.OrphanEscalation);
    [Fact] void should_retain_the_tombstone() => _result.Incident.ShouldEqual(_current);
}
