// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_a_removal_changes_the_cached_tail : given.an_administrator_roster
{
    bool _before;
    bool _after;

    async Task Establish()
    {
        await AddUser(_first, "root");
        SeedGrant(_first);
        _before = await _roster.IsActiveAdministrator(_first);
        _events.ClearReceivedCalls();
        Seed(_first, new UserRemoved());
    }

    async Task Because() => _after = await _roster.IsActiveAdministrator(_first);

    [Fact] void should_have_cached_a_real_grant() => _before.ShouldBeTrue();
    [Fact] void should_refuse_the_removed_user_before_its_projection_catches_up() => _after.ShouldBeFalse();
    [Fact] void should_replay_the_changed_tail() => _events.ReceivedCalls().Count(call => call.GetMethodInfo().Name == "GetRange").ShouldEqual(1);
}
