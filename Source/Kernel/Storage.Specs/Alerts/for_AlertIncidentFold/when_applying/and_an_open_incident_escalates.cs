// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Storage.Alerts.for_AlertIncidentFold.when_applying;

public class and_an_open_incident_escalates : given.a_recorded_transition
{
    void Establish()
    {
        _current = AlertIncidentFold.Apply(null, _raise).Incident;
        _transition = _raise with { Kind = AlertIncidentTransitionKind.Escalated, Target = _raise.Target with { Partition = "changed" }, SequenceNumber = 11UL, Occurred = _raise.Occurred.AddMinutes(1), Severity = AlertSeverity.Critical };
    }

    void Because() => _result = AlertIncidentFold.Apply(_current, _transition);

    [Fact] void should_retain_original_target() => _result.Incident.Target.ShouldEqual(_raise.Target);
    [Fact] void should_retain_raise_time() => _result.Incident.RaisedAt.ShouldEqual((DateTimeOffset?)_raise.Occurred);
    [Fact] void should_retain_raise_position() => _result.Incident.RaisedSequenceNumber.ShouldEqual(_raise.SequenceNumber);
    [Fact] void should_advance_transition_position() => _result.Incident.LastTransitionSequenceNumber.Value.ShouldEqual(11UL);
}
