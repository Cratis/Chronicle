// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Roster = Cratis.Chronicle.Concepts.Security.AdministratorRoster;

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_unsigned_history_requires_operator_recovery : given.an_administrator_roster
{
    bool _before;
    bool _afterFirst;
    bool _afterSecond;
    int _automaticAppends;

    async Task Establish()
    {
        await AddUser(_first, "root");
        await AddUser(_second, "operator");
        SeedForged(Roster.EventSourceId, new UserGrantedAdministrator(_first, GrantReason.Bootstrap, "spec"));
        _before = await _roster.IsActiveAdministrator(_first);
        await _roster.EnsureAdministratorPrivilege();
        await _roster.Grant(_first, GrantReason.Bootstrap, "spec");
        _automaticAppends = _appendCalls;
        ConfigureRecovery("operator", "fresh");
    }

    async Task Because()
    {
        await _roster.EnsureAdministratorPrivilege();
        _afterFirst = await _roster.IsActiveAdministrator(_first);
        _afterSecond = await _roster.IsActiveAdministrator(_second);
    }

    [Fact] void should_deny_the_unsigned_grant() => _before.ShouldBeFalse();
    [Fact] void should_not_append_automatically() => _automaticAppends.ShouldEqual(0);
    [Fact] void should_grant_only_the_recovery_user() => (!_afterFirst && _afterSecond).ShouldBeTrue();
}
