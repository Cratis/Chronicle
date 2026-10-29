// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security.for_InitialAdminSetupTokens;

public class when_validating_an_empty_token_with_an_empty_configured_token : given.setup_tokens
{
    void Establish() => _tokens = new(new EphemeralDataProtectionProvider(), Options.Create(new Configuration.ChronicleOptions
    {
        Authentication = new() { InitialAdminSetupToken = string.Empty }
    }));
    void Because() => _valid = _tokens.IsValid(_userId, InitialAdminSetupToken.Empty);
    [Fact] void should_reject_the_token() => _valid.ShouldBeFalse();
}
