// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Statistics.for_StatisticsReader;

public class when_authentication_is_disabled_for_http_system_statistics : given.an_http_statistics_reader
{
    EventStatistics _result;

    protected override Authentication AuthenticationOptions => new() { Enabled = false };

    void Establish() => _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

    async Task Because()
    {
        BeginRequest();
        _result = await EventStatistics.StatisticsForNamespace(_reader, "System", "first");
    }

    [Fact] void should_return_the_statistics() => _result.TotalEvents.ShouldEqual(3);
    [Fact] async Task should_not_consult_the_administrator_roster() => await _privileges.DidNotReceive().IsActiveAdministrator(Arg.Any<UserId>());
}
