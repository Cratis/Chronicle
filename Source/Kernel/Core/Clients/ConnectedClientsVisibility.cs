// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Security;
using Cratis.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Represents an implementation of <see cref="IConnectedClientsVisibility"/>.
/// </summary>
/// <param name="options">The server configuration.</param>
/// <param name="ownership">The <see cref="IConnectionOwnership"/> telling who opened each connection.</param>
/// <param name="httpContextAccessor">The <see cref="IHttpContextAccessor"/> for the current request.</param>
[Singleton]
public class ConnectedClientsVisibility(
    IOptions<ChronicleOptions> options,
    IConnectionOwnership ownership,
    IHttpContextAccessor httpContextAccessor) : IConnectedClientsVisibility
{
    /// <inheritdoc/>
    public ConnectionCaller CurrentCaller() =>
        ConnectionCaller.For(httpContextAccessor.HttpContext, options.Value.Authentication.Enabled);

    /// <inheritdoc/>
    public async Task<IEnumerable<ConnectedClient>> Filter(IEnumerable<ConnectedClient> clients, ConnectionCaller caller)
    {
        if (!options.Value.Authentication.Enabled || caller.Owner.Equals(ConnectionOwner.InProcess)) return clients;
        if (caller.Owner.IsUnidentified) return [];
        if (await IsAdministrator(caller)) return clients;

        return [.. clients.Where(client => ownership.IsOwnedBy(client.ConnectionId, caller.Owner))];
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ConnectedClient>> FilterForCurrentRequest(IEnumerable<ConnectedClient> clients) =>
        Filter(clients, CurrentCaller());

    static async Task<bool> IsAdministrator(ConnectionCaller caller)
    {
        if (caller.Principal is null || caller.RequestServices is null) return false;

        var authorization = caller.RequestServices.GetRequiredService<IAuthorizationService>();
        var result = await authorization.AuthorizeAsync(caller.Principal, WellKnownAuthorizationPolicies.Administrator);
        return result.Succeeded;
    }
}
