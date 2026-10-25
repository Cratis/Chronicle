// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_granting_privilege_while_the_user_is_removed : given.an_administrator_roster
{
    bool _removedAdministrator;

    async Task Establish()
    {
        await AddUser(_first, "root");
        _synchronizeNextAppends = true;
    }

    async Task Because()
    {
        await Catch.Exception(() => Task.WhenAll(
            _roster.Grant(_first, GrantReason.Bootstrap, "spec"), CreateRoster().RemoveUser(_first)));
        _removedAdministrator = Recorded<UserRemoved>().Any() && await CreateRoster().IsActiveAdministrator(_first);
    }

    [Fact] void should_protect_the_grant_target_against_concurrent_removal() => _conflicts.ShouldEqual(1);
    [Fact] void should_not_grant_privilege_to_a_removed_user_even_with_a_stale_active_user_projection() => _removedAdministrator.ShouldBeFalse();
}
