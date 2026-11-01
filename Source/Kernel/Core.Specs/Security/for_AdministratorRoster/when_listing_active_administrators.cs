// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_listing_active_administrators : given.an_administrator_roster
{
    IEnumerable<UserId> _administrators;

    async Task Establish()
    {
        await AddUser(_first, "active");
        await AddUser(_second, "inactive", active: false);
        SeedGrant(_first);
        SeedGrant(_second);
        SeedGrant(Guid.NewGuid());
    }

    async Task Because() => _administrators = await _roster.GetActiveAdministrators();

    [Fact] void should_list_only_existing_active_users_with_authoritative_grants() => _administrators.ToArray().ShouldEqual([_first]);
}
