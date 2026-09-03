// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Security;

using StoredUser = Cratis.Chronicle.Storage.Security.User;

namespace Cratis.Chronicle.Security.for_SetInitialAdminPassword;

public class when_targeting_the_configured_administrator : Specification
{
    static readonly Guid _administratorId = Guid.NewGuid();

    IGrainFactory _grainFactory;
    IStorage _storage;
    IUserStorage _users;
    IEventSequence _eventLog;
    IEventSerializer _eventSerializer;
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
            Id = _administratorId,
            Username = "chronicle-root",
            HasLoggedIn = false
        });
        _eventLog = Substitute.For<IEventSequence>();
        _eventSerializer = Substitute.For<IEventSerializer>();
        _eventSerializer.Serialize(Arg.Any<object>()).Returns(new JsonObject());
        _grainFactory.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(_eventLog);
        _eventLog.Append(
            Arg.Any<EventSourceType>(),
            Arg.Any<EventSourceId>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventType>(),
            Arg.Any<JsonObject>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Causation>>(),
            Arg.Any<Identity>(),
            Arg.Any<IEnumerable<Tag>>(),
            Arg.Any<ConcurrencyScope>()).Returns(AppendResult.Success(CorrelationId.New(), EventSequenceNumber.First));
        _command = new(_administratorId, "a-secure-password", "a-secure-password");
    }

    async Task Because() =>
        _exception = await Catch.Exception(() => _command.Handle(
            _grainFactory,
            _storage,
            new Configuration.Authentication
            {
                DefaultAdminUsername = "admin",
                AdminUser = new() { Username = "chronicle-root" }
            },
            _eventSerializer));

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_serialize_password_changed() => _eventSerializer.Received(1).Serialize(Arg.Any<UserPasswordChanged>());
    [Fact] void should_append_password_changed() => _eventLog.Received(1).Append(
        Arg.Any<EventSourceType>(),
        Arg.Is<EventSourceId>(id => id == _administratorId),
        Arg.Any<EventStreamType>(),
        Arg.Any<EventStreamId>(),
        Arg.Is<EventType>(eventType => eventType == typeof(UserPasswordChanged).GetEventType()),
        Arg.Any<JsonObject>(),
        Arg.Any<CorrelationId>(),
        Arg.Any<IEnumerable<Causation>>(),
        Arg.Any<Identity>(),
        Arg.Any<IEnumerable<Tag>>(),
        Arg.Is<ConcurrencyScope>(scope =>
            scope.EventSourceId &&
            scope.SequenceNumber == EventSequenceNumber.BeforeFirst &&
            scope.EventTypes!.Single() == typeof(UserPasswordChanged).GetEventType()));
}
