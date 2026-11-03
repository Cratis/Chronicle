// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_removing_two_administrators_concurrently : given.an_administrator_roster
{
    bool[] _results;
    bool[] _remaining;

    async Task Establish()
    {
        await AddUser(_first, "first");
        await AddUser(_second, "second");
        SeedGrant(_first);
        SeedGrant(_second);
        _synchronizeNextAppends = true;
    }

    async Task Because()
    {
        _results = await Task.WhenAll(_roster.RemoveUser(_first), CreateRoster().RemoveUser(_second));
        var reactivated = CreateRoster();
        _remaining = [await reactivated.IsActiveAdministrator(_first), await reactivated.IsActiveAdministrator(_second)];
    }

    [Fact] void should_reload_after_a_concurrency_violation() => _conflicts.ShouldEqual(1);
    [Fact] void should_remove_only_one_user() => _results.Count(removed => removed).ShouldEqual(1);
    [Fact] void should_retain_one_administrator_after_reactivation() => _remaining.Count(active => active).ShouldEqual(1);
    [Fact] void should_record_only_one_removal() => Recorded<UserRemoved>().Count().ShouldEqual(1);
    [Fact] void should_record_only_one_revocation() => Recorded<UserRevokedAdministrator>().Count().ShouldEqual(1);
}
