// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Limits connection metadata to the caller's connections unless administrator access is granted.
/// </summary>
/// <param name="options">The server configuration.</param>
/// <param name="owners">The call's principal resolver.</param>
/// <param name="ownership">Local connection ownership.</param>
/// <param name="authorization">The administrator authorization policy.</param>
/// <param name="httpContextAccessor">The current HTTP request.</param>
public class ConnectedClientsVisibility(
    IOptions<ChronicleOptions> options,
    IConnectionOwnerResolver owners,
    IConnectionOwnership ownership,
    IAuthorizationService authorization,
    IHttpContextAccessor httpContextAccessor) : IConnectedClientsVisibility
{
    /// <inheritdoc/>
    public Func<IEnumerable<ConnectedClient>, Task<IEnumerable<ConnectedClient>>> Capture(ClaimsPrincipal? principal = null, bool hasTransport = false)
    {
        if (!hasTransport)
        {
            principal ??= owners.Principal ?? httpContextAccessor.HttpContext?.User;
            hasTransport = owners.HasTransport || httpContextAccessor.HttpContext is not null;
        }

        var owner = owners.Resolve(principal, hasTransport);
        var unfiltered = !options.Value.Authentication.Enabled || owner == ConnectionOwner.InProcess;
        return clients => Filter(clients, principal, owner, unfiltered);
    }

    async Task<IEnumerable<ConnectedClient>> Filter(IEnumerable<ConnectedClient> clients, ClaimsPrincipal? principal, ConnectionOwner owner, bool unfiltered)
    {
        if (unfiltered || (principal is not null && (await authorization.AuthorizeAsync(principal, null, WellKnownAuthorizationPolicies.Administrator)).Succeeded)) return clients;
        if (owner == ConnectionOwner.Unidentified) return [];

        return clients.Where(client => ownership.IsOwnedBy(client.ConnectionId, owner)).ToArray();
    }
}
