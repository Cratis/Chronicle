// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_migrating_a_proven_legacy_administrator : given.an_administrator_roster
{
    async Task Establish()
    {
        await AddUser(_first, "root");
        Seed(_first, new InitialAdminUserAdded("ROOT", "root@example.com"));
        _synchronizeNextAppends = true;
    }

    async Task Because() => await Task.WhenAll(_roster.EnsureAdministratorPrivilege(), CreateRoster().EnsureAdministratorPrivilege());

    [Fact] void should_grant_the_legacy_random_user_id() => Recorded<UserGrantedAdministrator>().Single().UserId.ShouldEqual(_first);
    [Fact] void should_record_the_provenance_reason() => Recorded<UserGrantedAdministrator>().Single().Reason.ShouldEqual(GrantReason.UpgradeProvenance);
    [Fact] void should_record_only_one_migration_marker() => Recorded<AdministratorPrivilegeMigrated>().Count().ShouldEqual(1);
    [Fact] void should_record_a_proven_outcome() => Recorded<AdministratorPrivilegeMigrated>().Single().Outcome.ShouldEqual("Proven");
}
