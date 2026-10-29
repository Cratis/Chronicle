// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_recovery_is_not_forced_and_an_administrator_exists : given.an_administrator_roster
{
    async Task Establish()
    {
        await AddUser(_first, "root");
        await AddUser(_second, "operator");
        SeedGrant(_first);
        ConfigureRecovery("operator", "unused-nonce");
    }

    async Task Because() => await _roster.EnsureAdministratorPrivilege();

    [Fact] void should_not_grant_operator_recovery() => Recorded<UserGrantedAdministrator>().Count(grant => grant.Reason == GrantReason.OperatorRecovery).ShouldEqual(0);
}
