// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Alerts.for_AlertIncidentFold.when_applying;

public class and_an_incident_is_raised : given.a_recorded_transition
{
    void Because() => _result = AlertIncidentFold.Apply(_current, _transition);

    [Fact] void should_create_a_complete_open_row() => _result.Incident.IsOpen.ShouldBeTrue();
    [Fact] void should_preserve_recorded_evidence() => _result.Incident.Evidence.ShouldEqual(_raise.Evidence);
    [Fact] void should_preserve_unknown_condition() => _result.Incident.Condition.ShouldEqual(_raise.Condition);
    [Fact] void should_preserve_literal_none_partition() => _result.Incident.Target.Partition.Value.ShouldEqual("[none]");
}
