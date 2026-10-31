// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security.for_SetInitialAdminPassword;

public class when_the_setup_token_is_missing : Specification
{
    IStorage _storage;
    IGrainFactory _grains;
    Exception _exception;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _grains = Substitute.For<IGrainFactory>();
    }

    async Task Because() => _exception = await Catch.Exception(() => new SetInitialAdminPassword(UserId.InitialAdministrator, "new-password", "new-password", null!)
        .Handle(_grains, _storage, Options.Create(new Configuration.ChronicleOptions()), Substitute.For<IEventSerializer>(), new InitialAdminSetupTokens(new EphemeralDataProtectionProvider(), Options.Create(new Configuration.ChronicleOptions())), CorrelationId.New()));

    [Fact] void should_require_a_token() => _exception.ShouldBeOfExactType<InvalidInitialAdminSetupToken>();
    [Fact] void should_not_read_user_state() => _storage.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_access_the_event_log() => _grains.DidNotReceive().GetGrain<IEventSequence>(Arg.Any<string>());
}
