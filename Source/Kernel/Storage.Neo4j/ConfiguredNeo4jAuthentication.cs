// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Neo4j.Driver;

namespace Cratis.Chronicle.Storage.Neo4j;

/// <summary>
/// Represents an <see cref="IAuthTokenManager"/> for the configured user name and password, or no authentication.
/// </summary>
/// <remarks>
/// Instances with the same user name and password are equal, so a driver is shared between them.
/// </remarks>
/// <param name="UserName">Optional user name.</param>
/// <param name="Password">Optional password.</param>
public sealed record ConfiguredNeo4jAuthentication(string? UserName, string? Password) : IAuthTokenManager
{
    /// <inheritdoc/>
    public ValueTask<IAuthToken> GetTokenAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Neo4jAuthentication.ToToken(UserName, Password, null));

    /// <inheritdoc/>
    ValueTask<bool> IAuthTokenManager.HandleSecurityExceptionAsync(IAuthToken token, SecurityException exception, CancellationToken cancellationToken) =>
        ValueTask.FromResult(false);

    /// <inheritdoc/>
    /// <remarks>Never includes the password.</remarks>
    public override string ToString() => $"{nameof(ConfiguredNeo4jAuthentication)} {{ UserName = {UserName}, HasPassword = {Password is not null} }}";
}
