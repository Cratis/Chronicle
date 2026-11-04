// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Roster = Cratis.Chronicle.Concepts.Security.AdministratorRoster;

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_recovering_after_an_unsigned_slot : given.an_administrator_roster
{
    UserId _third;
    bool _before;
    bool _after;

    async Task Establish()
    {
        _third = Guid.NewGuid();
        await AddUser(_first, "root");
        await AddUser(_second, "operator");
        await AddUser(_third, "managed");
        SeedGrant(_first);
        SeedForged(Roster.EventSourceId, new UserGrantedAdministrator(_third, GrantReason.AdministratorGrant, "spec"));
        _before = await _roster.IsActiveAdministrator(_first) || await _roster.IsActiveAdministrator(_third);
        ConfigureRecovery("operator", "fresh");
    }

    async Task Because()
    {
        await _roster.EnsureAdministratorPrivilege();
        await _roster.Grant(_third, GrantReason.AdministratorGrant, "spec");
        _after = await _roster.IsActiveAdministrator(_second) && await _roster.IsActiveAdministrator(_third);
    }

    [Fact] void should_suspend_prior_privileges() => _before.ShouldBeFalse();
    [Fact] void should_allow_management_grants_after_recovery() => _after.ShouldBeTrue();
    [Fact] void should_not_exhaust_concurrency() => _conflicts.ShouldEqual(0);
}
