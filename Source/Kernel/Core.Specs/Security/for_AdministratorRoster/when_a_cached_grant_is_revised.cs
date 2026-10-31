// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Roster = Cratis.Chronicle.Concepts.Security.AdministratorRoster;

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_a_cached_grant_is_revised : given.an_administrator_roster
{
    bool _before;
    bool _afterFirst;
    bool _afterSecond;

    async Task Establish()
    {
        await AddUser(_first, "root");
        await AddUser(_second, "second");
        SeedGrant(_first);
        var slot = Seed(Roster.EventSourceId, new UserGrantedAdministrator(_second, GrantReason.Bootstrap, "spec"));
        _before = await _roster.IsActiveAdministrator(_first);
        ReviseSlot(slot.Context.SequenceNumber, _contents[slot]);
    }

    async Task Because()
    {
        _afterFirst = await _roster.IsActiveAdministrator(_first);
        _afterSecond = await _roster.IsActiveAdministrator(_second);
    }

    [Fact] void should_start_with_privilege() => _before.ShouldBeTrue();
    [Fact] void should_suspend_both_users_after_revision() => (_afterFirst || _afterSecond).ShouldBeFalse();
}
