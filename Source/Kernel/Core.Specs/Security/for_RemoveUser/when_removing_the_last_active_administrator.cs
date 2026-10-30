// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_RemoveUser;

public class when_removing_the_last_active_administrator : Specification
{
    IGrainFactory _grainFactory;
    IAdministratorRoster _roster;
    RemoveUser _command;
    Exception _error;

    void Establish()
    {
        _command = new RemoveUser(Guid.NewGuid());
        _grainFactory = Substitute.For<IGrainFactory>();
        _roster = Substitute.For<IAdministratorRoster>();
        _roster.RemoveUser(_command.UserId).Returns(false);
        _grainFactory.GetGrain<IAdministratorRoster>(Concepts.Security.AdministratorRoster.Key).Returns(_roster);
    }

    async Task Because() => _error = await Catch.Exception(() => _command.Handle(_grainFactory));

    [Fact] void should_refuse_the_removal() => _error.ShouldBeOfExactType<LastAdministratorCannotBeRemoved>();
    [Fact] void should_route_removal_through_the_coordinator() => _roster.Received(1).RemoveUser(_command.UserId);
}
