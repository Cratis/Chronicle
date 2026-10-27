// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_reusing_a_forced_recovery_nonce_after_revocation : given.an_administrator_roster
{
    bool _administrator;

    async Task Establish()
    {
        await AddUser(_first, "root");
        await AddUser(_second, "operator");
        SeedGrant(_first);
        ConfigureRecovery("operator", "consumed-nonce", force: true);
    }

    async Task Because()
    {
        await _roster.EnsureAdministratorPrivilege();
        await _roster.Revoke(_second, "operator revoked");
        var reactivated = CreateRoster();
        await reactivated.EnsureAdministratorPrivilege();
        _administrator = await reactivated.IsActiveAdministrator(_second);
    }

    [Fact] void should_not_regrant_a_revoked_privilege() => _administrator.ShouldBeFalse();
    [Fact] void should_keep_one_audited_recovery_grant() => Recorded<UserGrantedAdministrator>().Count(grant => grant.Reason == GrantReason.OperatorRecovery).ShouldEqual(1);
    [Fact] void should_record_explicit_force() => Recorded<UserGrantedAdministrator>().Single(grant => grant.Reason == GrantReason.OperatorRecovery).Evidence.ShouldContain("\"Force\":true");
}
