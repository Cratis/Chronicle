// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Queries;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security.for_SystemStoreQueryFilter;

public class when_a_store_scoped_query_addresses_an_ordinary_store : given.a_query_filter
{
    const string PlainQuery = "Cratis.Chronicle.Sequences.AppendedEvent.QueryEvents";
    const string AlertQuery = "Cratis.Chronicle.Alerts.AlertIncidentPage.GetOpenIncidents";

    public static TheoryData<string, bool> Modes() => new()
    {
        { PlainQuery, true },
        { PlainQuery, false },
        { AlertQuery, true },
        { AlertQuery, false }
    };

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task should_follow_the_enforcement_mode_when_the_capability_is_not_granted(string query, bool enforce)
    {
        var result = await Perform(query, Decisions(granted: false), enforce);
        if (enforce)
        {
            result.ShouldNotBeNull();
            result.IsAuthorized.ShouldBeFalse();
        }
        else
        {
            result.ShouldBeNull();
        }
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task should_allow_a_granted_query(string query, bool enforce)
    {
        var result = await Perform(query, Decisions(granted: true), enforce);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task should_decide_read_for_a_query_without_a_declared_capability()
    {
        var decisions = Decisions(granted: true);
        await Perform(PlainQuery, decisions, true);
        await decisions.Received(1).Decide(Arg.Any<ClaimsPrincipal?>(), Arg.Is<Capability>(c => c == WellKnownCapabilities.ReadEventStore), Arg.Is<AuthorizationScope>(s => s == AuthorizationScope.ForNamespace("shop", "tenant")));
    }

    [Fact]
    public async Task should_keep_the_declared_capability_of_an_alert_query()
    {
        var decisions = Decisions(granted: true);
        await Perform(AlertQuery, decisions, true);
        await decisions.Received(1).Decide(Arg.Any<ClaimsPrincipal?>(), Arg.Is<Capability>(c => c == WellKnownCapabilities.ReadAlerts), Arg.Any<AuthorizationScope>());
    }

    [Fact]
    public async Task should_not_decide_a_query_without_an_event_store()
    {
        var decisions = Decisions(granted: false);
        var result = await Perform("Cratis.Chronicle.Security.User.AllUsers", decisions, true, store: false);
        result.ShouldBeNull();
        decisions.ReceivedCalls().ShouldBeEmpty();
    }

    static IAuthorizationDecisions Decisions(bool granted)
    {
        var decisions = Substitute.For<IAuthorizationDecisions>();
        decisions.Decide(Arg.Any<ClaimsPrincipal?>(), Arg.Any<Capability>(), Arg.Any<AuthorizationScope>()).Returns(call =>
            granted
                ? AuthorizationDecision.Allow(AuthorizationReason.Granted, call.ArgAt<Capability>(1), call.ArgAt<AuthorizationScope>(2))
                : AuthorizationDecision.Deny(AuthorizationReason.NotGranted, call.ArgAt<Capability>(1), call.ArgAt<AuthorizationScope>(2)));
        return decisions;
    }

    Task<QueryResult> Perform(string query, IAuthorizationDecisions decisions, bool enforce, bool store = true)
    {
        var services = new ServiceCollection()
            .AddSingleton(decisions)
            .AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(NullLoggerFactory.Instance)
            .AddSingleton(Options.Create(new ChronicleOptions { Authentication = new Authentication { EnforceStoreEntitlements = enforce } }));
        var queryArguments = store
            ? new QueryArguments { ["eventStore"] = "shop", ["namespace"] = "tenant" }
            : new QueryArguments { ["id"] = "1" };
        return _filter.OnPerform(new QueryContext(query, CorrelationId.New(), Paging.NotPaged, Sorting.None, queryArguments, ServiceProvider: services.BuildServiceProvider()));
    }
}
