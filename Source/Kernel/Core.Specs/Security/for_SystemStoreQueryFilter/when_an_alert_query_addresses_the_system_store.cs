// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_SystemStoreQueryFilter;

public class when_an_alert_query_addresses_the_system_store : given.a_query_filter
{
    public static TheoryData<string> Callers() => Security.given.a_system_store_caller.NonAdministrators();

    [Theory]
    [MemberData(nameof(Callers))]
    public async Task should_refuse_a_caller_who_is_not_an_administrator(string caller)
    {
        _caller = CallerNamed(caller);
        foreach (var query in new[] { "Cratis.Chronicle.Alerts.AlertIncidentPage.GetOpenIncidents", "Cratis.Chronicle.Alerts.AlertIncidentLookup.GetOpenIncident", "Cratis.Chronicle.Alerts.AlertIncidentSummary.GetOpenIncidentCounts" })
        {
            var result = await _filter.OnPerform(ContextFor(query, ("eventStore", "System")));
            result.IsAuthorized.ShouldBeFalse();
        }
    }
}
