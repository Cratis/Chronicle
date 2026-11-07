// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;

namespace Cratis.Chronicle.Storage.Vault;

/// <summary>
/// Creates the clients Chronicle uses to talk to HashiCorp Vault.
/// </summary>
internal static class VaultClients
{
    /// <summary>
    /// Creates a client for an address, authenticating with the token in <c language="csharp">VAULT_TOKEN</c>.
    /// </summary>
    /// <param name="address">The Vault address.</param>
    /// <returns>The <see cref="VaultClient"/>.</returns>
    /// <exception cref="InvalidOperationException">There is no token in the environment.</exception>
    internal static VaultClient Create(string address)
    {
        var token = Environment.GetEnvironmentVariable("VAULT_TOKEN");
        if (string.IsNullOrEmpty(token))
        {
            throw new InvalidOperationException(
                "VAULT_TOKEN environment variable is required for HashiCorp Vault authentication. " +
                "Set this variable to a valid Vault token before starting Chronicle.");
        }

        var authMethod = new TokenAuthMethodInfo(token);
        var settings = new VaultClientSettings(address, authMethod);
        return new VaultClient(settings);
    }
}
