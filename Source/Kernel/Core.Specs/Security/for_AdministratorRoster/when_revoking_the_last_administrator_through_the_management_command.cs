// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_revoking_the_last_administrator_through_the_management_command : given.an_administrator_roster
{
    Exception _error;

    async Task Establish()
    {
        await AddUser(_first, "root");
        SeedGrant(_first);
        _grainFactory.GetGrain<IAdministratorRoster>(Concepts.Security.AdministratorRoster.Key).Returns(_roster);
    }

    async Task Because() => _error = await Catch.Exception(() =>
        new RevokeAdministrator(_first, "attempted removal").Handle(_grainFactory, Substitute.For<ICurrentPrincipalAccessor>()));

    [Fact] void should_reject_revocation() => _error.ShouldBeOfExactType<LastAdministratorCannotBeRemoved>();
    [Fact] void should_not_record_a_revocation() => Recorded<UserRevokedAdministrator>().ShouldBeEmpty();
}
