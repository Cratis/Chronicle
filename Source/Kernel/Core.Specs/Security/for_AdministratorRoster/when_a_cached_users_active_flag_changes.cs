// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_a_cached_users_active_flag_changes : given.an_administrator_roster
{
    bool _before;
    bool _after;

    async Task Establish()
    {
        await AddUser(_first, "root");
        SeedGrant(_first);
        _before = await _roster.IsActiveAdministrator(_first);
        var user = await _storage.System.Users.GetById(_first);
        user!.IsActive = false;
        await _storage.System.Users.Update(user);
    }

    async Task Because() => _after = await _roster.IsActiveAdministrator(_first);

    [Fact] void should_have_cached_a_real_grant() => _before.ShouldBeTrue();
    [Fact] void should_not_cache_the_active_user_flag() => _after.ShouldBeFalse();
}
