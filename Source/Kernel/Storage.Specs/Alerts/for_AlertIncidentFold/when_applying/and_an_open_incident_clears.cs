// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Storage.Alerts.for_AlertIncidentFold.when_applying;

public class and_an_open_incident_clears : given.a_recorded_transition
{
    void Establish()
    {
        _current = AlertIncidentFold.Apply(null, _raise).Incident;
        _transition = _raise with { Kind = AlertIncidentTransitionKind.Cleared, SequenceNumber = 11UL, ClearedReason = AlertClearedReason.Recovered };
    }

    void Because() => _result = AlertIncidentFold.Apply(_current, _transition);

    [Fact] void should_close_the_row() => _result.Incident.IsOpen.ShouldBeFalse();
    [Fact] void should_retain_raise_evidence() => _result.Incident.Evidence.ShouldEqual(_raise.Evidence);
    [Fact] void should_preserve_the_recorded_reason() => _result.Incident.ClearedReason.ShouldEqual((AlertClearedReason?)AlertClearedReason.Recovered);
}
