// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Security;

using StoredUser = Cratis.Chronicle.Storage.Security.User;

namespace Cratis.Chronicle.Security.for_SetInitialAdminPassword;

public class when_targeting_a_non_administrator : Specification
{
    IGrainFactory _grainFactory;
    IStorage _storage;
    IUserStorage _users;
    SetInitialAdminPassword _command;
    Exception _exception;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _storage = Substitute.For<IStorage>();
        _users = Substitute.For<IUserStorage>();
        _storage.System.Users.Returns(_users);
        _users.GetById(Arg.Any<Concepts.Security.UserId>()).Returns(new StoredUser
        {
            Id = Guid.NewGuid(),
            Username = "someone-else",
            HasLoggedIn = false
        });
        _command = new(Guid.NewGuid(), "a-secure-password", "a-secure-password");
    }

    async Task Because() =>
        _exception = await Catch.Exception(() => _command.Handle(
            _grainFactory,
            _storage,
            new Configuration.Authentication { DefaultAdminUsername = "admin" },
            Substitute.For<EventSequences.IEventSerializer>()));

    [Fact] void should_reject_the_target() => _exception.ShouldBeOfExactType<Services.Security.InitialPasswordCanOnlyBeSetForAdministrator>();
}
