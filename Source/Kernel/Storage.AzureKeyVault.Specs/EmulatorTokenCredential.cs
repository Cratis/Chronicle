// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Core;

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>
/// Supplies the emulator token fetched through the fixture's trusted HTTP client.
/// </summary>
/// <param name="token">The token returned by the emulator.</param>
sealed class EmulatorTokenCredential(string token) : TokenCredential
{
    readonly AccessToken _token = new(token, DateTimeOffset.UtcNow.AddDays(1));

    /// <inheritdoc/>
    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) => _token;

    /// <inheritdoc/>
    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) => ValueTask.FromResult(_token);
}
