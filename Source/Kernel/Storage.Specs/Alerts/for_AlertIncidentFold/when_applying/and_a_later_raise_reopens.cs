// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Alerts.for_AlertIncidentFold.when_applying;

public class and_a_later_raise_reopens : given.a_recorded_transition
{
    void Establish()
    {
        _current = AlertIncidentFold.Apply(null, _raise with { Kind = AlertIncidentTransitionKind.Cleared }).Incident;
        _transition = _raise with { SequenceNumber = 11UL, Occurred = _raise.Occurred.AddMinutes(1) };
    }

    void Because() => _result = AlertIncidentFold.Apply(_current, _transition);

    [Fact] void should_reopen_the_row() => _result.Incident.IsOpen.ShouldBeTrue();
    [Fact] void should_reset_raise_position() => _result.Incident.RaisedSequenceNumber.Value.ShouldEqual(11UL);
    [Fact] void should_reset_raise_time() => _result.Incident.RaisedAt.ShouldEqual((DateTimeOffset?)_transition.Occurred);
    [Fact] void should_remove_the_clear_reason() => _result.Incident.ClearedReason.ShouldBeNull();
}
