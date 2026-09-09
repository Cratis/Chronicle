// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security.for_SetInitialAdminPassword;

public class when_the_legacy_administrator_already_has_credentials : Specification
{
    IStorage _storage;
    UserId _userId;
    Exception _exception;

    void Establish()
    {
        _userId = Guid.NewGuid();
        _storage = Substitute.For<IStorage>();
        var user = new Storage.Security.User { Id = _userId, Username = "admin", HasLoggedIn = false, PasswordHash = "legacy-hash" };
        _storage.System.Users.GetById(_userId).Returns(user);
        _storage.System.Users.GetByUsername("admin").Returns(user);
    }

    async Task Because() => _exception = await Catch.Exception(() => new SetInitialAdminPassword(_userId, "new-password", "new-password")
        .Handle(Substitute.For<IGrainFactory>(), _storage, Options.Create(new Configuration.ChronicleOptions()), Substitute.For<IEventSerializer>()));

    [Fact] void should_not_allow_anonymous_credential_replacement() => _exception.ShouldBeOfExactType<InitialAdminPasswordAlreadyUsed>();
}
