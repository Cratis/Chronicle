// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Services.for_StoreEntitlementEnforcement;

public class when_the_same_principal_is_decided_through_both_transports : given.a_store_entitlement_caller
{
    const string Generic = "Cratis.Chronicle.Sequences.SomeReadModel.GetSomething";

    public static TheoryData<string, bool, bool> Cases()
    {
        var cases = new TheoryData<string, bool, bool>();
        foreach (var query in new[]
        {
            "Cratis.Chronicle.Alerts.AlertIncidentPage.GetOpenIncidents",
            "Cratis.Chronicle.Alerts.AlertIncidentLookup.GetOpenIncident",
            "Cratis.Chronicle.Alerts.AlertIncidentSummary.GetOpenIncidentCounts",
            Generic
        })
        {
            foreach (var enforce in new[] { true, false })
            {
                foreach (var granted in new[] { true, false })
                {
                    cases.Add(query, enforce, granted);
                }
            }
        }

        return cases;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task should_give_the_same_verdict(string query, bool enforce, bool granted)
    {
        _granted = granted;
        var context = ContextFor(enforce);
        var services = SystemStoreGuard.HttpContextFor(context).RequestServices;
        var capability = StoreCapabilityDecision.CapabilityOf(query);

        var grpc = await StoreCapabilityGuard.Permits(context, new("shop", "tenant", Capability: capability));
        var http = await new SystemStoreQueryFilter().OnPerform(new QueryContext(
            query,
            CorrelationId.New(),
            Paging.NotPaged,
            Sorting.None,
            new QueryArguments { ["eventStore"] = "shop", ["namespace"] = "tenant" },
            ServiceProvider: services)) is null;

        http.ShouldEqual(grpc);
    }
}
