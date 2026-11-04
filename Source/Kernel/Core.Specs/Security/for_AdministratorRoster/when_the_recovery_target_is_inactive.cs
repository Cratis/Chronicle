// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_the_recovery_target_is_inactive : given.an_administrator_roster
{
    async Task Establish()
    {
        await AddUser(_first, "operator", active: false);
        ConfigureRecovery("operator", "nonce", force: true);
    }

    async Task Because() => await _roster.EnsureAdministratorPrivilege();

    [Fact] void should_not_grant_any_privilege() => Recorded<UserGrantedAdministrator>().ShouldBeEmpty();
}
