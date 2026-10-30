// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security.for_AdministratorPrivileges;

public class when_checking_a_user : Specification
{
    AdministratorPrivileges _privileges;
    IAdministratorRoster _roster;
    UserId _userId;
    bool _administrator;

    void Establish()
    {
        _userId = Guid.NewGuid();
        _roster = Substitute.For<IAdministratorRoster>();
        _roster.IsActiveAdministrator(_userId).Returns(true);
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IAdministratorRoster>(Concepts.Security.AdministratorRoster.Key).Returns(_roster);
        _privileges = new AdministratorPrivileges(grainFactory);
    }

    async Task Because() => _administrator = await _privileges.IsActiveAdministrator(_userId);

    [Fact] void should_consult_the_authoritative_coordinator() => _roster.Received(1).IsActiveAdministrator(_userId);
    [Fact] void should_return_the_authoritative_result() => _administrator.ShouldBeTrue();
}
