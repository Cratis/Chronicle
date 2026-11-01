// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Security.given;

namespace Cratis.Chronicle.Services.for_QueryExecutor;

public class when_executing_a_routed_query : given.a_routed_query
{
    public static TheoryData<string> SystemSpellings() => new() { "System", "SYSTEM", "system", "SyStEm" };

    public static TheoryData<string, string, string?> SeparatorValues() => new()
    {
        { "a#b", "Default", null },
        { "bench", "Default#System", null },
        { "bench", "Default", "event-log#System#Default" }
    };

    [Fact]
    public async Task should_run_a_query_on_another_store_without_asking_for_the_roster()
    {
        var probe = new Probe();
        var result = await QueryExecutor.Execute(CallFor(probe, CallerNamed(OrdinaryUser)), new QueryRouting("bench", "Default"), probe.Run);

        result.IsAuthorized.ShouldBeTrue();
        probe.Invocations.ShouldEqual(1);
        probe.AccessResolutions.ShouldEqual(0);
    }

    [Fact]
    public async Task should_run_an_observable_query_on_another_store_without_asking_for_the_roster()
    {
        var probe = new Probe();
        var result = await QueryExecutor.Execute(CallFor(probe, CallerNamed(OrdinaryUser)), new QueryRouting("bench", "Default"), probe.Observe).FirstAsync();

        result.IsAuthorized.ShouldBeTrue();
        probe.AccessResolutions.ShouldEqual(0);
    }

    [Theory]
    [MemberData(nameof(SystemSpellings))]
    public async Task should_run_a_system_query_for_an_administrator(string store)
    {
        var probe = new Probe();
        var result = await QueryExecutor.Execute(CallFor(probe, CallerNamed(Administrator)), new QueryRouting(store, "Default"), probe.Run);

        result.IsAuthorized.ShouldBeTrue();
        probe.Invocations.ShouldEqual(1);
    }

    [Theory]
    [MemberData(nameof(a_system_store_caller.NonAdministrators), MemberType = typeof(a_system_store_caller))]
    public async Task should_refuse_a_system_query_for_a_caller_who_is_not_an_administrator(string name)
    {
        var probe = new Probe();
        var result = await QueryExecutor.Execute(CallFor(probe, CallerNamed(name)), new QueryRouting("SYSTEM", "Default"), probe.Run);

        result.IsAuthorized.ShouldBeFalse();
        probe.Invocations.ShouldEqual(0);
    }

    [Theory]
    [MemberData(nameof(a_system_store_caller.NonAdministrators), MemberType = typeof(a_system_store_caller))]
    public async Task should_refuse_an_observable_system_query_for_a_caller_who_is_not_an_administrator(string name)
    {
        var probe = new Probe();
        var results = await QueryExecutor.Execute(CallFor(probe, CallerNamed(name)), new QueryRouting("System", "Default"), probe.Observe).ToList();

        results.Count.ShouldEqual(1);
        results[0].IsAuthorized.ShouldBeFalse();
        probe.Invocations.ShouldEqual(0);
    }

    [Fact]
    public async Task should_refuse_a_system_query_without_a_request()
    {
        var probe = new Probe();
        var result = await QueryExecutor.Execute(CallFor(probe, CallerNamed(Administrator), served: false), new QueryRouting("System"), probe.Run);

        result.IsAuthorized.ShouldBeFalse();
        probe.Invocations.ShouldEqual(0);
    }

    [Fact]
    public async Task should_refuse_a_system_query_for_a_call_without_a_server_context()
    {
        var probe = new Probe();
        var result = await QueryExecutor.Execute(default, new QueryRouting("System"), probe.Run);

        result.IsAuthorized.ShouldBeFalse();
        probe.Invocations.ShouldEqual(0);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task should_apply_authentication_configuration_without_a_server_context(bool authenticationEnabled, bool allowed)
    {
        _authentication = new global::Cratis.Chronicle.Configuration.Authentication { Enabled = authenticationEnabled };
        var previous = SystemStoreGuard.InProcessServices;
        SystemStoreGuard.InProcessServices = CreateServices();
        try
        {
            var probe = new Probe();
            var result = await QueryExecutor.Execute(default, new QueryRouting("System"), probe.Run);
            var observed = await QueryExecutor.Execute(default, new QueryRouting("System"), probe.Observe).FirstAsync();

            result.IsAuthorized.ShouldEqual(allowed);
            observed.IsAuthorized.ShouldEqual(allowed);
            probe.Invocations.ShouldEqual(allowed ? 2 : 0);
            _privileges.ReceivedCalls().ShouldBeEmpty();
        }
        finally
        {
            SystemStoreGuard.InProcessServices = previous;
        }
    }

    [Theory]
    [MemberData(nameof(SeparatorValues))]
    public async Task should_refuse_a_routing_value_with_the_separator_for_an_administrator(string store, string first, string? second)
    {
        var probe = new Probe();
        var result = await QueryExecutor.Execute(CallFor(probe, CallerNamed(Administrator)), new QueryRouting(store, first, second), probe.Run);

        result.ValidationResults.Count.ShouldEqual(1);
        result.ValidationResults[0].Message.ShouldEqual(SystemStoreAccess.SeparatorRefused);
        probe.Invocations.ShouldEqual(0);
        probe.AccessResolutions.ShouldEqual(0);
    }

    [Fact]
    public async Task should_refuse_the_separator_in_an_observable_query()
    {
        var probe = new Probe();
        var results = await QueryExecutor.Execute(CallFor(probe, CallerNamed(Administrator)), new QueryRouting("bench", "a#b"), probe.Observe).ToList();

        results.Count.ShouldEqual(1);
        results[0].ValidationResults.Count.ShouldEqual(1);
        probe.Invocations.ShouldEqual(0);
    }
}
