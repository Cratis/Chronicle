// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Alerts;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsReactor.when_handling;

public class and_storage_fails : given.an_incidents_reactor
{
    Exception? _error;

    void Establish()
    {
        _incidents.Apply(Arg.Any<AlertIncidentTransition>()).Returns(Task.FromException<AlertIncidentWriteOutcome>(new AlertIncidentWriteNotConfirmed(_raised.IncidentId)));
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _reactor.Raised(_raised, _context));
    }

    [Fact] void should_propagate_the_storage_failure() => _error.ShouldBeOfExactType<AlertIncidentWriteNotConfirmed>();
}
