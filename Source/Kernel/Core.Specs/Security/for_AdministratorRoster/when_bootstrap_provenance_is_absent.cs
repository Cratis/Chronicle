// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_bootstrap_provenance_is_absent : given.an_administrator_roster
{
    bool _privilegedBeforeRestart;
    bool _privilegedAfterRestart;
    bool _sentinelPrivileged;

    async Task Establish() => await AddUser(_first, "root");

    async Task Because()
    {
        await _roster.EnsureAdministratorPrivilege();
        _privilegedBeforeRestart = await _roster.IsActiveAdministrator(_first);
        Seed(_first, new InitialAdminUserAdded("root", "root@example.com"));
        var restarted = CreateRoster();
        await restarted.EnsureAdministratorPrivilege();
        _privilegedAfterRestart = await restarted.IsActiveAdministrator(_first);
        _sentinelPrivileged = await restarted.IsActiveAdministrator(UserId.NotSet);
    }

    [Fact] void should_not_promote_a_same_named_account() => Recorded<UserGrantedAdministrator>().ShouldBeEmpty();
    [Fact] void should_not_retry_an_unproven_migration() => Recorded<AdministratorPrivilegeMigrated>().Count().ShouldEqual(1);
    [Fact] void should_record_an_unproven_outcome() => Recorded<AdministratorPrivilegeMigrated>().Single().Outcome.ShouldEqual("Unproven");
    [Fact] void should_record_the_user_sentinel() => Recorded<AdministratorPrivilegeMigrated>().Single().UserId.ShouldEqual(UserId.NotSet);
    [Fact] void should_not_grant_privilege_before_restart() => _privilegedBeforeRestart.ShouldBeFalse();
    [Fact] void should_not_grant_privilege_after_restart() => _privilegedAfterRestart.ShouldBeFalse();
    [Fact] void should_not_grant_privilege_to_the_sentinel() => _sentinelPrivileged.ShouldBeFalse();
}
