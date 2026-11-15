// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Security.for_SystemStoreQueryFilter;

public class when_an_alert_query_addresses_an_ordinary_store : given.a_query_filter
{
    public static TheoryData<string> Queries() =>
    [
        "Cratis.Chronicle.Alerts.AlertIncidentPage.GetOpenIncidents",
        "Cratis.Chronicle.Alerts.AlertIncidentLookup.GetOpenIncident",
        "Cratis.Chronicle.Alerts.AlertIncidentSummary.GetOpenIncidentCounts",
        "Cratis.Chronicle.Alerts.AlertIncidentPage"
    ];

    [Theory]
    [MemberData(nameof(Queries))]
    public async Task should_refuse_when_the_decision_fails(string query)
    {
        var result = await Perform(query, Decision(AuthorizationReason.Failed, false));
        result.ShouldNotBeNull();
        result.IsAuthorized.ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(Queries))]
    public async Task should_refuse_when_the_decision_throws(string query)
    {
        var decisions = Substitute.For<IAuthorizationDecisions>();
        decisions.Decide(Arg.Any<System.Security.Claims.ClaimsPrincipal?>(), Arg.Any<Capability>(), Arg.Any<AuthorizationScope>()).Returns<AuthorizationDecision>(_ => throw new InvalidOperationException("unavailable"));
        var result = await Perform(query, decisions);
        result.ShouldNotBeNull();
        result.IsAuthorized.ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(Queries))]
    public async Task should_refuse_when_the_capability_is_unknown(string query)
    {
        var result = await Perform(query, Decision(AuthorizationReason.UnknownCapability, false));
        result.ShouldNotBeNull();
        result.IsAuthorized.ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(Queries))]
    public async Task should_tolerate_a_decision_that_is_not_granted(string query)
    {
        var result = await Perform(query, Decision(AuthorizationReason.NotGranted, false));
        result.ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(Queries))]
    public async Task should_allow_a_granted_decision_and_decide_at_the_addressed_store(string query)
    {
        var decisions = Decision(AuthorizationReason.Granted, true);
        var result = await Perform(query, decisions);
        result.ShouldBeNull();
        await decisions.Received(1).Decide(Arg.Any<System.Security.Claims.ClaimsPrincipal?>(), Arg.Is<Capability>(c => c == WellKnownCapabilities.ReadAlerts), Arg.Is<AuthorizationScope>(s => s == AuthorizationScope.ForNamespace("shop", "tenant")));
    }

    static IAuthorizationDecisions Decision(AuthorizationReason reason, bool allowed)
    {
        var decisions = Substitute.For<IAuthorizationDecisions>();
        decisions.Decide(Arg.Any<System.Security.Claims.ClaimsPrincipal?>(), Arg.Any<Capability>(), Arg.Any<AuthorizationScope>()).Returns(call =>
            allowed
                ? AuthorizationDecision.Allow(reason, call.ArgAt<Capability>(1), call.ArgAt<AuthorizationScope>(2))
                : AuthorizationDecision.Deny(reason, call.ArgAt<Capability>(1), call.ArgAt<AuthorizationScope>(2)));
        return decisions;
    }

    Task<QueryResult> Perform(string query, IAuthorizationDecisions decisions)
    {
        var services = new ServiceCollection()
            .AddSingleton(decisions)
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .BuildServiceProvider();
        var queryArguments = new QueryArguments { ["eventStore"] = "shop", ["namespace"] = "tenant" };
        return _filter.OnPerform(new QueryContext(query, CorrelationId.New(), Paging.NotPaged, Sorting.None, queryArguments, ServiceProvider: services));
    }
}
