// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Statistics.for_StatisticsReader;

public class when_an_external_administrator_reads_http_system_statistics : given.an_http_statistics_reader
{
    EventStatistics _result;

    protected override Authentication AuthenticationOptions => new()
    {
        Authority = "https://authority.example",
        ExternalAdministratorClaim = new() { Type = "roles", Value = "chronicle-admin" }
    };

    void Establish() => _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("roles", "chronicle-admin")], ExternalAuthority.AuthenticationType));

    async Task Because()
    {
        BeginRequest();
        _result = await EventStatistics.StatisticsForNamespace(_reader, "System", "first");
    }

    [Fact] void should_return_the_statistics() => _result.TotalEvents.ShouldEqual(3);
    [Fact] async Task should_not_consult_the_internal_roster() => await _privileges.DidNotReceive().IsActiveAdministrator(Arg.Any<UserId>());
}
