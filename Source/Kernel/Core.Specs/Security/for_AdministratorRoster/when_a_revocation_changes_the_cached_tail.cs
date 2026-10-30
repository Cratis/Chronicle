// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Roster = Cratis.Chronicle.Concepts.Security.AdministratorRoster;

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_a_revocation_changes_the_cached_tail : given.an_administrator_roster
{
    bool _before;
    bool _after;

    async Task Establish()
    {
        await AddUser(_first, "root");
        SeedGrant(_first);
        _before = await _roster.IsActiveAdministrator(_first);
        _events.ClearReceivedCalls();
        Seed(Roster.EventSourceId, new UserRevokedAdministrator(_first, "revoked on another node"));
    }

    async Task Because() => _after = await _roster.IsActiveAdministrator(_first);

    [Fact] void should_have_cached_a_real_grant() => _before.ShouldBeTrue();
    [Fact] void should_refuse_the_revoked_user_on_the_next_check() => _after.ShouldBeFalse();
    [Fact] void should_replay_the_changed_tail() => _events.ReceivedCalls().Count(call => call.GetMethodInfo().Name == "GetRange").ShouldEqual(1);
}
