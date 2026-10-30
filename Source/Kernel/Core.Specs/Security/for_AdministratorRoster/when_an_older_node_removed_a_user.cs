// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_an_older_node_removed_a_user : given.an_administrator_roster
{
    bool _administrator;

    async Task Establish()
    {
        await AddUser(_first, "root");
        SeedGrant(_first);
        Seed(_first, new UserRemoved());
    }

    async Task Because() => _administrator = await _roster.IsActiveAdministrator(_first);

    [Fact] void should_treat_the_removal_as_an_implicit_revocation_even_before_user_projection_catches_up() => _administrator.ShouldBeFalse();
}
