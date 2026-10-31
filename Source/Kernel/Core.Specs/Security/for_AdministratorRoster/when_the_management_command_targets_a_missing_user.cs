// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_the_management_command_targets_a_missing_user : given.an_administrator_roster
{
    Exception _error;

    void Establish() => _grainFactory.GetGrain<IAdministratorRoster>(Concepts.Security.AdministratorRoster.Key).Returns(_roster);

    async Task Because() => _error = await Catch.Exception(() => new GrantAdministrator(_first)
        .Handle(_grainFactory, Substitute.For<ICurrentPrincipalAccessor>()));

    [Fact] void should_reject_the_grant() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_append_a_grant() => Recorded<UserGrantedAdministrator>().ShouldBeEmpty();
}
