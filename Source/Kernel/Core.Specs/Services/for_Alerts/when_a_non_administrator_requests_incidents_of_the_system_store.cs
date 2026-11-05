// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Security.given;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.Alerts.for_Alerts;

public class when_a_non_administrator_requests_incidents_of_the_system_store : for_QueryExecutor.given.a_routed_query
{
    public static TheoryData<string> Callers() => a_system_store_caller.NonAdministrators();

    [Theory]
    [MemberData(nameof(Callers))]
    public async Task should_refuse_every_query_without_touching_storage(string caller)
    {
        var storage = Substitute.For<IStorage>();
        var alerts = new Alerts(storage, Substitute.For<IAlertIncidentsReadiness>(), NullLogger<Alerts>.Instance);
        var context = CallFor(new(), CallerNamed(caller));

        var lookup = await alerts.GetOpenIncident(new() { EventStore = "System", IncidentId = Guid.NewGuid() }, context);
        var page = await alerts.GetOpenIncidents(new() { EventStore = "sYsTeM" }, context);
        var counts = await alerts.GetOpenIncidentCounts(new() { EventStore = "SYSTEM" }, context);

        lookup.IsAuthorized.ShouldBeFalse();
        page.IsAuthorized.ShouldBeFalse();
        counts.IsAuthorized.ShouldBeFalse();
        storage.ReceivedCalls().ShouldBeEmpty();
    }
}
