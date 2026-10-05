// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Storage.Alerts;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsReactor.when_handling;

public class and_an_escalation_is_recorded : given.an_incidents_reactor
{
    async Task Because()
    {
        await _reactor.Escalated(new(_raised.IncidentId, _raised.Condition, AlertSeverity.Critical, _raised.Target, _raised.Evidence), _context);
    }

    [Fact] async Task should_retain_context_metadata() => await _incidents.Received(1).Apply(Arg.Is<AlertIncidentTransition>(transition => transition.Kind == AlertIncidentTransitionKind.Escalated && transition.SequenceNumber == _context.SequenceNumber && transition.Occurred == _context.Occurred));
}
