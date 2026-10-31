// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_the_bootstrap_user_was_later_removed : given.an_administrator_roster
{
    async Task Establish()
    {
        await AddUser(_first, "root");
        Seed(_first, new InitialAdminUserAdded("root", "root@example.com"));
        Seed(_first, new UserRemoved());
    }

    async Task Because() => await _roster.EnsureAdministratorPrivilege();

    [Fact] void should_not_grant_even_when_the_user_projection_is_stale() => Recorded<UserGrantedAdministrator>().ShouldBeEmpty();
    [Fact] void should_record_unproven_provenance() => Recorded<AdministratorPrivilegeMigrated>().Single().Outcome.ShouldEqual("Unproven");
}
