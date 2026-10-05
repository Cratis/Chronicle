// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Alerts;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsReactor.when_handling;

public class and_a_raise_is_recorded : given.an_incidents_reactor
{
    async Task Because()
    {
        await _reactor.Raised(_raised, _context);
    }

    [Fact] async Task should_persist_in_system_default() => await _incidents.Received(1).Apply(Arg.Is<AlertIncidentTransition>(transition => transition == _raised.ToTransition(_context)));
    [Fact] void should_not_resolve_the_affected_store() => _storage.DidNotReceive().GetEventStore((EventStoreName)"affected");
}
