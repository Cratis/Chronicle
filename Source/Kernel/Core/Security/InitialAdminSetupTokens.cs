// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Configuration;
using Cratis.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents an implementation of <see cref="IInitialAdminSetupTokens"/>.
/// </summary>
/// <param name="provider">The shared data protection provider.</param>
/// <param name="options">The Chronicle options.</param>
[Singleton]
public class InitialAdminSetupTokens(IDataProtectionProvider provider, IOptions<ChronicleOptions> options) : IInitialAdminSetupTokens
{
    readonly IDataProtector _protector = provider.CreateProtector("Cratis.Chronicle.Security.InitialAdminSetup");

    /// <inheritdoc/>
    public InitialAdminSetupToken Issue(UserId userId) => _protector.Protect(userId.Value.ToString());

    /// <inheritdoc/>
    public bool IsValid(UserId userId, InitialAdminSetupToken? token)
    {
        if (token is null || string.IsNullOrWhiteSpace(token.Value))
        {
            return false;
        }

        var configured = options.Value.Authentication.InitialAdminSetupToken;
        if (!string.IsNullOrWhiteSpace(configured) && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(configured), Encoding.UTF8.GetBytes(token.Value)))
        {
            return true;
        }

        try
        {
            return _protector.Unprotect(token.Value) == userId.Value.ToString();
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}
