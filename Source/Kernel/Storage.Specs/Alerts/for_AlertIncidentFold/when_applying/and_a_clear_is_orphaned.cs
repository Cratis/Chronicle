// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Storage.Alerts.for_AlertIncidentFold.when_applying;

public class and_a_clear_is_orphaned : given.a_recorded_transition
{
    void Establish()
    {
        _transition = _raise with { Kind = AlertIncidentTransitionKind.Cleared, ClearedReason = AlertClearedReason.Removed };
    }

    void Because() => _result = AlertIncidentFold.Apply(_current, _transition);

    [Fact] void should_retain_a_closed_tombstone() => _result.Incident.IsOpen.ShouldBeFalse();
    [Fact] void should_not_invent_raise_evidence() => _result.Incident.Evidence.ShouldBeNull();
    [Fact] void should_not_invent_raise_position() => _result.Incident.RaisedSequenceNumber.ShouldBeNull();
    [Fact] void should_not_invent_severity() => _result.Incident.Severity.ShouldBeNull();
}
