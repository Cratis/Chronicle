// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Storage.Alerts;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsReactor.when_handling;

public class and_an_escalation_is_orphaned : given.an_incidents_reactor
{
    Exception? _error;

    void Establish()
    {
        _incidents.Apply(Arg.Any<AlertIncidentTransition>()).Returns(AlertIncidentWriteOutcome.OrphanEscalation);
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _reactor.Escalated(new(_raised.IncidentId, _raised.Condition, AlertSeverity.Critical, _raised.Target, _raised.Evidence), _context));
    }

    [Fact] void should_acknowledge_without_retry() => _error.ShouldBeNull();
}
