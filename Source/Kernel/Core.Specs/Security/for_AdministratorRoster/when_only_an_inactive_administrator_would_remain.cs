// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_only_an_inactive_administrator_would_remain : given.an_administrator_roster
{
    bool _removed;

    async Task Establish()
    {
        await AddUser(_first, "active");
        await AddUser(_second, "inactive", active: false);
        SeedGrant(_first);
        SeedGrant(_second);
    }

    async Task Because() => _removed = await _roster.RemoveUser(_first);

    [Fact] void should_not_count_inactive_privileges_as_active_administrators() => _removed.ShouldBeFalse();
    [Fact] void should_not_append_a_user_removal() => Recorded<UserRemoved>().ShouldBeEmpty();
}
