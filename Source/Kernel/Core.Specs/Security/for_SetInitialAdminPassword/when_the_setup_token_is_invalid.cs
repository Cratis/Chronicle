// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security.for_SetInitialAdminPassword;

public class when_the_setup_token_is_invalid : Specification
{
    IStorage _storage;
    IGrainFactory _grainFactory;
    IEventSerializer _serializer;
    UserId _userId;
    Exception _exception;

    void Establish()
    {
        _userId = UserId.InitialAdministrator;
        _storage = Substitute.For<IStorage>();
        var user = new Storage.Security.User { Id = _userId, Username = "admin", HasLoggedIn = false };
        _storage.System.Users.GetById(_userId).Returns(user);
        _storage.System.Users.GetByUsername("admin").Returns(user);
        _grainFactory = Substitute.For<IGrainFactory>();
        _serializer = Substitute.For<IEventSerializer>();
    }

    async Task Because() => _exception = await Catch.Exception(() => new SetInitialAdminPassword(_userId, "new-password", "new-password", "invalid")
        .Handle(_grainFactory, _storage, Options.Create(new Configuration.ChronicleOptions()), _serializer, Substitute.For<IInitialAdminSetupTokens>(), CorrelationId.New()));

    [Fact] void should_require_a_valid_token() => _exception.ShouldBeOfExactType<InvalidInitialAdminSetupToken>();
    [Fact] void should_not_serialize_a_password() => _serializer.DidNotReceive().Serialize(Arg.Any<object>());
    [Fact] void should_not_access_the_event_log() => _grainFactory.DidNotReceive().GetGrain<IEventSequence>(Arg.Any<string>());
}
