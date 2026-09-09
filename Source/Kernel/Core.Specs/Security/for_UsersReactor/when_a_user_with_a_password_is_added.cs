// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Security;
using Microsoft.Extensions.Logging;

using StoredUser = Cratis.Chronicle.Storage.Security.User;

namespace Cratis.Chronicle.Security.for_UsersReactor;

public class when_a_user_with_a_password_is_added : Specification
{
    static readonly EventSourceId _userId = Guid.NewGuid();

    IUserStorage _users;
    UsersReactor _reactor;
    StoredUser _createdUser;

    void Establish()
    {
        _users = Substitute.For<IUserStorage>();
        _users.Create(Arg.Any<StoredUser>()).Returns(callInfo =>
        {
            _createdUser = callInfo.Arg<StoredUser>();
            return Task.CompletedTask;
        });
        _reactor = new UsersReactor(_users, Substitute.For<ILogger<UsersReactor>>());
    }

    async Task Because() =>
        await _reactor.Added(
            new UserAdded("new-user", "user@example.com", "password-hash"),
            EventContext.Empty with { EventSourceId = _userId });

    [Fact] void should_create_the_correct_user() => (_createdUser.Id.Value == Guid.Parse(_userId.Value)).ShouldBeTrue();
    [Fact] void should_mark_the_password_as_initialized() => _createdUser.HasLoggedIn.ShouldBeTrue();
    [Fact] void should_require_the_first_password_change() => _createdUser.RequiresPasswordChange.ShouldBeTrue();
}
