// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Alerts.for_AlertIncidentFold.when_applying;

public class and_the_transition_is_stale : given.a_recorded_transition
{
    void Establish()
    {
        _current = AlertIncidentFold.Apply(null, _raise).Incident;
        _transition = _raise with { Kind = AlertIncidentTransitionKind.Cleared, SequenceNumber = 9UL };
    }

    void Because() => _result = AlertIncidentFold.Apply(_current, _transition);

    [Fact] void should_not_change_the_row() => _result.Incident.ShouldEqual(_current);
}
