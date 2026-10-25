// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_the_bootstrap_user_is_inactive : given.an_administrator_roster
{
    async Task Establish()
    {
        await AddUser(_first, "root", active: false);
        Seed(_first, new InitialAdminUserAdded("root", "root@example.com"));
    }

    async Task Because() => await _roster.EnsureAdministratorPrivilege();

    [Fact] void should_not_grant_a_privilege() => Recorded<UserGrantedAdministrator>().ShouldBeEmpty();
}
