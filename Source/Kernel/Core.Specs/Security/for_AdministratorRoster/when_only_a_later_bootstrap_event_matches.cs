// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_only_a_later_bootstrap_event_matches : given.an_administrator_roster
{
    async Task Establish()
    {
        await AddUser(_first, "different");
        await AddUser(_second, "root");
        Seed(_first, new InitialAdminUserAdded("different", "first@example.com"));
        Seed(_second, new InitialAdminUserAdded("root", "second@example.com"));
    }

    async Task Because() => await _roster.EnsureAdministratorPrivilege();

    [Fact] void should_not_choose_a_later_matching_event() => Recorded<UserGrantedAdministrator>().ShouldBeEmpty();
    [Fact] void should_record_unproven_provenance() => Recorded<AdministratorPrivilegeMigrated>().Single().Outcome.ShouldEqual("Unproven");
}
