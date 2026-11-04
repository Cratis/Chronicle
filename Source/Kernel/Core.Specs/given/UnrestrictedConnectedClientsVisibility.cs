// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.given;

/// <summary>
/// Preserves the complete client listing for specifications unrelated to caller visibility.
/// </summary>
public class UnrestrictedConnectedClientsVisibility : IConnectedClientsVisibility
{
    /// <inheritdoc/>
    public Func<IEnumerable<ConnectedClient>, Task<IEnumerable<ConnectedClient>>> Capture(ClaimsPrincipal? principal = null, bool hasTransport = false) => clients => Task.FromResult(clients);
}
