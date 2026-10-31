// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security.for_InitialAdminSetupTokens.given;

public class setup_tokens : Specification
{
    protected InitialAdminSetupTokens _tokens;
    protected UserId _userId = UserId.InitialAdministrator;
    protected InitialAdminSetupToken _token;
    protected bool _valid;

    void Establish() => _tokens = new(new EphemeralDataProtectionProvider(), Options.Create(new Configuration.ChronicleOptions
    {
        Authentication = new() { InitialAdminSetupToken = "configured-token" }
    }));
}
