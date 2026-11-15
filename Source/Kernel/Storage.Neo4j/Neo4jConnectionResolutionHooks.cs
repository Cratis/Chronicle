// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Neo4j.Driver;

namespace Cratis.Chronicle.Storage.Neo4j;

/// <summary>
/// Represents the hooks that let a Neo4j driver follow a resolved connection after it has been created.
/// </summary>
/// <remarks>
/// <para>
/// As an <see cref="IAuthTokenManager"/> it hands the driver the resolved identity whenever the driver opens a connection.
/// The resolution is cached by the resolver, so this only resolves again when the cache is empty or expired. That is how a
/// rotated credential or an expiring token reaches a driver that lives for the lifetime of the process.
/// </para>
/// <para>
/// When the server rejects the identity, or the token has expired, it drops the cached resolution so the next connection
/// picks up rotated credentials, and tells the driver to try again.
/// </para>
/// <para>
/// Keep one instance per request: the driver is shared by server and authentication manager.
/// </para>
/// </remarks>
/// <param name="resolver">The <see cref="ICachingConnectionResolver"/> to resolve through.</param>
/// <param name="request">The <see cref="ConnectionRequest"/> to resolve.</param>
/// <param name="configured">The <see cref="ConfiguredNeo4jAuthentication"/> used when the resolution gives no identity.</param>
public class Neo4jConnectionResolutionHooks(ICachingConnectionResolver resolver, ConnectionRequest request, ConfiguredNeo4jAuthentication configured) : IAuthTokenManager
{
    const string Unauthorized = "Neo.ClientError.Security.Unauthorized";
    const string TokenExpired = "Neo.ClientError.Security.TokenExpired";

    /// <inheritdoc/>
    public async ValueTask<IAuthToken> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        var resolved = await resolver.Resolve(request, cancellationToken);
        return resolved.Identity is null
            ? await configured.GetTokenAsync(cancellationToken)
            : Neo4jAuthentication.ToToken(resolved.Identity);
    }

    /// <inheritdoc/>
    public ValueTask<bool> HandleSecurityExceptionAsync(IAuthToken token, SecurityException exception, CancellationToken cancellationToken = default)
    {
        if (exception.Code is not (Unauthorized or TokenExpired))
        {
            return ValueTask.FromResult(false);
        }

        resolver.Invalidate(request);
        return ValueTask.FromResult(true);
    }
}
