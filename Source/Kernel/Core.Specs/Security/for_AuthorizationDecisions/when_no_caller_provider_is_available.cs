// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security.for_AuthorizationDecisions;

public class when_no_caller_provider_is_available : Specification
{
    [Fact]
    public async Task should_refuse_administrator_decisions() =>
        (await Decisions().Decide(Caller(), WellKnownCapabilities.AdministerSecurity, AuthorizationScope.Kernel)).Reason.ShouldEqual(AuthorizationReason.Failed);

    [Theory]
    [InlineData("first", "north")]
    [InlineData("second", "south")]
    public async Task should_leave_ordinary_operations_in_audit_mode(string store, string space) =>
        (await Decisions().Decide(Caller(), WellKnownCapabilities.OperateEventStore, AuthorizationScope.ForNamespace(store, space))).Reason.ShouldEqual(AuthorizationReason.NotEnrolled);

    [Fact]
    public async Task should_decide_distinct_stores_concurrently()
    {
        var decisions = Decisions();
        var results = await Task.WhenAll(
            Task.Run(() => decisions.Decide(Caller(), WellKnownCapabilities.OperateEventStore, AuthorizationScope.ForNamespace("first", "north"))),
            Task.Run(() => decisions.Decide(Caller(), WellKnownCapabilities.OperateEventStore, AuthorizationScope.ForNamespace("second", "south"))));
        results.Select(result => result.Reason).ShouldEqual([AuthorizationReason.NotEnrolled, AuthorizationReason.NotEnrolled]);
    }

    static ClaimsPrincipal Caller() => new(new ClaimsIdentity([new Claim("sub", "app")], "spec"));
    static AuthorizationDecisions Decisions() => new(Options.Create(new Configuration.ChronicleOptions()), NullLogger<AuthorizationDecisions>.Instance);
}
