// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Storage.Alerts;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsReactor.when_handling;

public class and_a_clear_is_recorded : given.an_incidents_reactor
{
    async Task Because()
    {
        await _reactor.Cleared(new(_raised.IncidentId, _raised.Condition, AlertClearedReason.Recovered, _raised.Target), _context);
    }

    [Fact] async Task should_persist_the_recorded_clear() => await _incidents.Received(1).Apply(Arg.Is<AlertIncidentTransition>(transition => transition.Kind == AlertIncidentTransitionKind.Cleared && transition.ClearedReason == AlertClearedReason.Recovered && transition.Severity == null));
}
