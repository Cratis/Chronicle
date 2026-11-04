// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Resolves connection owners from the call's transport identity.
/// </summary>
/// <param name="options">The server configuration.</param>
[Singleton]
public class ConnectionOwnerResolver(IOptions<ChronicleOptions> options) : IConnectionOwnerResolver
{
    readonly AsyncLocal<ClaimsPrincipal?> _principal = new();
    readonly AsyncLocal<bool> _hasTransport = new();

    /// <inheritdoc/>
    public ClaimsPrincipal? Principal
    {
        get => _principal.Value;
        set => _principal.Value = value;
    }

    /// <inheritdoc/>
    public ConnectionOwner Current() => Resolve(Principal, HasTransport);

    /// <inheritdoc/>
    public bool HasTransport
    {
        get => _hasTransport.Value;
        set => _hasTransport.Value = value;
    }

    /// <inheritdoc/>
    public ConnectionOwner Resolve(ClaimsPrincipal? principal, bool hasTransport = false)
    {
        if (!options.Value.Authentication.Enabled) return ConnectionOwner.Anonymous;

        return principal is null && !hasTransport ? ConnectionOwner.InProcess : ConnectionOwner.From(principal);
    }
}
