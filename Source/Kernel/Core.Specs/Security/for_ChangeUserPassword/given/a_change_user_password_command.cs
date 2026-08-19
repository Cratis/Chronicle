// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Security;
using Microsoft.AspNetCore.Identity;

using StoredUser = Cratis.Chronicle.Storage.Security.User;

namespace Cratis.Chronicle.Security.for_ChangeUserPassword.given;

public class a_change_user_password_command : Specification
{
    protected static readonly Guid UserIdentifier = Guid.Parse("6d8b1c2a-3e4f-4a5b-8c9d-0e1f2a3b4c5d");
    protected const string OldPassword = "the-old-password";
    protected const string NewPassword = "the-new-password";

    protected IGrainFactory _grainFactory;
    protected IStorage _storage;
    protected IUserStorage _users;
    protected IEventSequence _eventLog;
    protected StoredUser _user;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _storage = Substitute.For<IStorage>();
        _users = Substitute.For<IUserStorage>();
        _storage.System.Users.Returns(_users);

        _eventLog = Substitute.For<IEventSequence>();
        _grainFactory.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(_eventLog);
        _eventLog.Append(
            Arg.Any<EventSourceId>(),
            Arg.Any<object>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Causation>>(),
            Arg.Any<Identity>(),
            Arg.Any<IEnumerable<Tag>>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>()).Returns(AppendResult.Success(CorrelationId.New(), EventSequenceNumber.First));

        _user = new StoredUser
        {
            Id = UserIdentifier,
            Username = "some-user",
            PasswordHash = new PasswordHasher<object>().HashPassword(null!, OldPassword)
        };
        _users.GetById(Arg.Any<Concepts.Security.UserId>()).Returns(_user);
    }
}
