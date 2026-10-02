// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Storage.Alerts.for_AlertIncidentFold.when_applying;

public class and_an_incident_is_raised_critical : given.a_recorded_transition
{
    void Establish()
    {
        _transition = _raise with { Severity = AlertSeverity.Critical };
    }

    void Because() => _result = AlertIncidentFold.Apply(_current, _transition);

    [Fact] void should_preserve_critical_without_an_escalation() => _result.Incident.Severity.ShouldEqual((AlertSeverity?)AlertSeverity.Critical);
}
