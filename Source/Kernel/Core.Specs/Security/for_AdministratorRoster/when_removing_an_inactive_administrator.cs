// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_removing_an_inactive_administrator : given.an_administrator_roster
{
    bool _removed;

    async Task Establish()
    {
        await AddUser(_first, "inactive", active: false);
        SeedGrant(_first);
    }

    async Task Because() => _removed = await _roster.RemoveUser(_first);

    [Fact] void should_allow_the_removal() => _removed.ShouldBeTrue();
    [Fact] void should_append_one_revocation() => Recorded<UserRevokedAdministrator>().Count().ShouldEqual(1);
    [Fact] void should_append_one_user_removal() => Recorded<UserRemoved>().Count().ShouldEqual(1);
    [Fact] void should_use_one_guarded_batch() => _appendCalls.ShouldEqual(1);
}
