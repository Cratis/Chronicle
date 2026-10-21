// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using System.Runtime.CompilerServices;
using Cratis.Arc.Connections;
using Cratis.Arc.EntityFrameworkCore.Connections;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.Connections;

/// <summary>
/// Represents a <see cref="DbConnectionInterceptor"/> that applies the resolved identity every time a connection opens.
/// </summary>
/// <remarks>
/// Only the identity is applied here: where the connection goes is already part of the connection string the options
/// were built with. Applying the identity on open means a rotated password or a refreshed access token takes effect
/// without new options. A failed login drops the cached resolution, so the next attempt resolves again.
/// </remarks>
/// <param name="resolver">The <see cref="ICachingConnectionResolver"/>.</param>
/// <param name="request">The <see cref="ConnectionRequest"/> the connection is for.</param>
internal sealed class ResolvedIdentityInterceptor(ICachingConnectionResolver resolver, ConnectionRequest request) : DbConnectionInterceptor
{
    readonly ConditionalWeakTable<DbConnection, string> _configured = [];

    /// <summary>
    /// Gets the request the connections are resolved for.
    /// </summary>
    internal ConnectionRequest Request => request;

    /// <inheritdoc/>
    public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
    {
        // EF Core opens synchronously here, so the resolution is waited for. It is cached, so this only waits when the
        // cache is empty or expired.
        Apply(connection, resolver.Resolve(request).GetAwaiter().GetResult());
        return result;
    }

    /// <inheritdoc/>
    public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
    {
        Apply(connection, await resolver.Resolve(request, cancellationToken));
        return result;
    }

    /// <inheritdoc/>
    public override void ConnectionFailed(DbConnection connection, ConnectionErrorEventData eventData) =>
        InvalidateOnAuthenticationFailure(eventData.Exception);

    /// <inheritdoc/>
    public override Task ConnectionFailedAsync(DbConnection connection, ConnectionErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        InvalidateOnAuthenticationFailure(eventData.Exception);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Apply the resolved identity to a connection Chronicle opens itself, outside of EF Core.
    /// </summary>
    /// <param name="connection">The closed <see cref="DbConnection"/>.</param>
    /// <returns>Awaitable task.</returns>
    internal async Task Prepare(DbConnection connection) => Apply(connection, await resolver.Resolve(request));

    void Apply(DbConnection connection, ResolvedConnection resolved)
    {
        var configured = _configured.GetValue(connection, static _ => _.ConnectionString);
        connection.ApplyResolvedConnection(configured, new ResolvedConnection(Identity: resolved.Identity));
    }

    void InvalidateOnAuthenticationFailure(Exception exception)
    {
        if (ConnectionResolutionInterceptor.IsAuthenticationFailure(exception))
        {
            resolver.Invalidate(request);
        }
    }
}
