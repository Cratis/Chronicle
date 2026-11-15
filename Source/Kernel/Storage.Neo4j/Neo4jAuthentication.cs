// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Neo4j.Driver;

namespace Cratis.Chronicle.Storage.Neo4j;

/// <summary>
/// Provides the conversion from an identity to the <see cref="IAuthToken"/> the driver authenticates with.
/// </summary>
static class Neo4jAuthentication
{
    /// <summary>
    /// Create the <see cref="IAuthToken"/>. An access token wins, then a user name with a password, else no authentication.
    /// </summary>
    /// <param name="userName">Optional user name.</param>
    /// <param name="password">Optional password.</param>
    /// <param name="accessToken">Optional access token, sent as a bearer token.</param>
    /// <returns>The <see cref="IAuthToken"/>.</returns>
    internal static IAuthToken ToToken(string? userName, string? password, string? accessToken) => (userName, password, accessToken) switch
    {
        (_, _, { } token) => AuthTokens.Bearer(token),
        ({ } user, { } secret, _) => AuthTokens.Basic(user, secret),
        _ => AuthTokens.None
    };

    /// <summary>
    /// Create the <see cref="IAuthToken"/> for a <see cref="ConnectionIdentity"/>.
    /// </summary>
    /// <param name="identity">The <see cref="ConnectionIdentity"/>.</param>
    /// <returns>The <see cref="IAuthToken"/>.</returns>
    internal static IAuthToken ToToken(ConnectionIdentity identity) => ToToken(identity.UserName, identity.Password, identity.AccessToken);
}
