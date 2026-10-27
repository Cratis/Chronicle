// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_revoking_two_administrators_concurrently : given.an_administrator_roster
{
    bool[] _results;

    async Task Establish()
    {
        await AddUser(_first, "first");
        await AddUser(_second, "second");
        SeedGrant(_first);
        SeedGrant(_second);
        _synchronizeNextAppends = true;
    }

    async Task Because() => _results = await Task.WhenAll(_roster.Revoke(_first, "spec"), CreateRoster().Revoke(_second, "spec"));

    [Fact] void should_reload_after_a_concurrency_violation() => _conflicts.ShouldEqual(1);
    [Fact] void should_revoke_only_one_privilege() => _results.Count(revoked => revoked).ShouldEqual(1);
    [Fact] void should_not_remove_a_user() => Recorded<UserRemoved>().ShouldBeEmpty();
    [Fact] void should_record_only_one_revocation() => Recorded<UserRevokedAdministrator>().Count().ShouldEqual(1);
}
