// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Statistics.for_StatisticsReader;

public class when_a_trusted_proxy_administrator_reads_system_statistics : given.an_http_statistics_reader
{
    EventStatistics _result;

    protected override Authentication AuthenticationOptions => new()
    {
        Http = new() { Mode = HttpAuthenticationMode.TrustedProxy, TrustedProxy = new() { AdministratorGroup = "administrators" } }
    };

    void Establish() => _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [new Claim(TrustedProxyUsers.GroupClaimType, "administrators")], TrustedProxyUsers.AuthenticationType));

    async Task Because()
    {
        BeginRequest();
        _result = await EventStatistics.StatisticsForEventStore(_reader, "System");
    }

    [Fact] void should_return_the_combined_total() => _result.TotalEvents.ShouldEqual(8);
    [Fact] void should_return_both_namespaces() => _result.Namespaces.ShouldEqual(2);
    [Fact] async Task should_not_consult_the_internal_roster() => await _privileges.DidNotReceive().IsActiveAdministrator(Arg.Any<UserId>());
}
