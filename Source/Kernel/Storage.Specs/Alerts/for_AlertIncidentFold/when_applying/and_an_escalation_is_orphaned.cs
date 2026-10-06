// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Alerts.for_AlertIncidentFold.when_applying;

public class and_an_escalation_is_orphaned : given.a_recorded_transition
{
    void Establish()
    {
        _transition = _raise with { Kind = AlertIncidentTransitionKind.Escalated };
    }

    void Because() => _result = AlertIncidentFold.Apply(_current, _transition);

    [Fact] void should_ignore_the_orphan() => _result.Outcome.ShouldEqual(AlertIncidentWriteOutcome.OrphanEscalation);
    [Fact] void should_not_create_a_row() => _result.Incident.ShouldBeNull();
}
