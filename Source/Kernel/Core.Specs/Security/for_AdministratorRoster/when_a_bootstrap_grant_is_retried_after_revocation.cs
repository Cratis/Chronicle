// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_a_bootstrap_grant_is_retried_after_revocation : given.an_administrator_roster
{
    bool _administrator;

    async Task Establish()
    {
        await AddUser(_first, "root");
        await AddUser(_second, "second");
        SeedGrant(_first);
        SeedGrant(_second);
    }

    async Task Because()
    {
        await _roster.Revoke(_first, "operator revocation");
        var reactivated = CreateRoster();
        await reactivated.Grant(_first, GrantReason.Bootstrap, "late retry");
        _administrator = await reactivated.IsActiveAdministrator(_first);
    }

    [Fact] void should_not_regrant_the_revoked_privilege() => _administrator.ShouldBeFalse();
    [Fact] void should_not_append_a_duplicate_bootstrap_grant() => Recorded<UserGrantedAdministrator>().Count(grant => grant.UserId == _first).ShouldEqual(1);
}
