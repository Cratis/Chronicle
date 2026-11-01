// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Kernel.Integration.for_DataProtection;

[Collection(ChronicleCollection.Name)]
public class when_rejecting_initial_setup_commands(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
{
    Exception _missing;
    Exception _invalid;
    Exception _wrongUser;
    Exception _completed;
    bool _unchanged;

    async Task Because()
    {
        var storage = Services.GetRequiredService<IStorage>();
        var grains = Services.GetRequiredService<IGrainFactory>();
        var serializer = Services.GetRequiredService<IEventSerializer>();
        var tokens = Services.GetRequiredService<IInitialAdminSetupTokens>();
        var user = new UserId(Guid.NewGuid());
        var username = $"setup-{Guid.NewGuid():N}";
        var options = Options.Create(new Configuration.ChronicleOptions
        {
            Authentication = new Configuration.Authentication { DefaultAdminUsername = username }
        });
        await storage.System.Users.Create(new Storage.Security.User { Id = user, Username = username });
        var validToken = tokens.Issue(user);
        _missing = await Catch.Exception(() => new SetInitialAdminPassword(user, "new-password", "new-password", InitialAdminSetupToken.Empty)
            .Handle(grains, storage, options, serializer, tokens, CorrelationId.New()));
        _invalid = await Catch.Exception(() => new SetInitialAdminPassword(user, "new-password", "new-password", "invalid")
            .Handle(grains, storage, options, serializer, tokens, CorrelationId.New()));
        _wrongUser = await Catch.Exception(() => new SetInitialAdminPassword(user, "new-password", "new-password", tokens.Issue(new UserId(Guid.NewGuid())))
            .Handle(grains, storage, options, serializer, tokens, CorrelationId.New()));
        await storage.System.Users.Update(new Storage.Security.User { Id = user, Username = username, HasLoggedIn = true, PasswordHash = "existing-hash" });
        _completed = await Catch.Exception(() => new SetInitialAdminPassword(user, "new-password", "new-password", validToken)
            .Handle(grains, storage, options, serializer, tokens, CorrelationId.New()));
        _unchanged = (await storage.System.Users.GetById(user))?.PasswordHash?.Value == "existing-hash";
    }

    [Fact] void should_reject_missing_tokens() => _missing.ShouldBeOfExactType<InvalidInitialAdminSetupToken>();
    [Fact] void should_reject_invalid_tokens() => _invalid.ShouldBeOfExactType<InvalidInitialAdminSetupToken>();
    [Fact] void should_reject_tokens_issued_for_another_user() => _wrongUser.ShouldBeOfExactType<InvalidInitialAdminSetupToken>();
    [Fact] void should_reject_completed_setup_even_with_a_valid_token() => _completed.ShouldBeOfExactType<InitialAdminPasswordAlreadyUsed>();
    [Fact] void should_preserve_existing_credentials() => _unchanged.ShouldBeTrue();
}
