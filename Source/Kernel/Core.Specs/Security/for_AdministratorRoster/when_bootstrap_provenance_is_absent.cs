// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_bootstrap_provenance_is_absent : given.an_administrator_roster
{
    async Task Establish() => await AddUser(_first, "root");

    async Task Because()
    {
        await _roster.EnsureAdministratorPrivilege();
        Seed(_first, new InitialAdminUserAdded("root", "root@example.com"));
        await CreateRoster().EnsureAdministratorPrivilege();
    }

    [Fact] void should_not_promote_a_same_named_account() => Recorded<UserGrantedAdministrator>().ShouldBeEmpty();
    [Fact] void should_not_retry_an_unproven_migration() => Recorded<AdministratorPrivilegeMigrated>().Count().ShouldEqual(1);
    [Fact] void should_record_an_unproven_outcome() => Recorded<AdministratorPrivilegeMigrated>().Single().Outcome.ShouldEqual("Unproven");
}
