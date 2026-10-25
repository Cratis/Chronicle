// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_querying_the_administrator_read_model : given.an_administrator_roster
{
    IEnumerable<Administrator> _administrators;

    async Task Establish()
    {
        await AddUser(_first, "root");
        await AddUser(_second, "ordinary-user");
        SeedGrant(_first);
        _grainFactory.GetGrain<IAdministratorRoster>(Concepts.Security.AdministratorRoster.Key).Returns(_roster);
    }

    async Task Because() => _administrators = await Administrator.AllAdministrators(_grainFactory, _storage);

    [Fact] void should_read_the_roster_instead_of_the_eventual_privilege_projection() => _administrators.Single().UserId.ShouldEqual(_first);
    [Fact] void should_join_the_user_name() => _administrators.Single().Username.ShouldEqual("root");
}
