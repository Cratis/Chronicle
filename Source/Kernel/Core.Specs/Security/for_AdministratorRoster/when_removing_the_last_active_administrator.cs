// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_removing_the_last_active_administrator : given.an_administrator_roster
{
    bool _removed;

    async Task Establish()
    {
        await AddUser(_first, "root");
        SeedGrant(_first);
    }

    async Task Because() => _removed = await _roster.RemoveUser(_first);

    [Fact] void should_refuse_the_removal() => _removed.ShouldBeFalse();
    [Fact] void should_not_append_a_removal() => Recorded<UserRemoved>().ShouldBeEmpty();
    [Fact] void should_not_append_a_revocation() => Recorded<UserRevokedAdministrator>().ShouldBeEmpty();
}
