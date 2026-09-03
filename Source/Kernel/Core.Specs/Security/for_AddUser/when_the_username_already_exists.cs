// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Security;

using StoredUser = Cratis.Chronicle.Storage.Security.User;

namespace Cratis.Chronicle.Security.for_AddUser;

public class when_the_username_already_exists : Specification
{
    IGrainFactory _grainFactory;
    IStorage _storage;
    IUserStorage _users;
    AddUser _command;
    Exception _exception;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _storage = Substitute.For<IStorage>();
        _users = Substitute.For<IUserStorage>();
        _storage.System.Users.Returns(_users);
        _users.GetByUsername(Arg.Any<Concepts.Security.Username>()).Returns(new StoredUser
        {
            Id = Guid.NewGuid(),
            Username = "existing-user"
        });
        _command = new(Guid.NewGuid(), "existing-user", "user@example.com", "password");
    }

    async Task Because() => _exception = await Catch.Exception(() => _command.Handle(_grainFactory, _storage));

    [Fact] void should_reject_the_user() => _exception.ShouldBeOfExactType<Services.Security.UserAlreadyExists>();
    [Fact] void should_not_append() => _grainFactory.DidNotReceive().GetGrain<EventSequences.IEventSequence>(Arg.Any<string>());
}
