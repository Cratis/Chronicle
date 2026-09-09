// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security.for_SetInitialAdminPassword;

public class when_confirmation_does_not_match : Specification
{
    Exception _exception;
    IStorage _storage;

    void Establish() => _storage = Substitute.For<IStorage>();

    async Task Because() => _exception = await Catch.Exception(() => new SetInitialAdminPassword(UserId.NotSet, "password-one", "password-two")
        .Handle(Substitute.For<IGrainFactory>(), _storage, Options.Create(new Configuration.ChronicleOptions()), Substitute.For<IEventSerializer>()));

    [Fact] void should_reject_the_confirmation() => _exception.ShouldBeOfExactType<PasswordConfirmationMismatch>();
    [Fact] void should_not_read_user_storage() => _storage.DidNotReceive().System.Users.GetById(Arg.Any<UserId>());
}
