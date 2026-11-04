// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Controls which connected clients a caller can see.
/// </summary>
public interface IConnectedClientsVisibility
{
    /// <summary>
    /// Captures the caller before asynchronous enumeration begins.
    /// </summary>
    /// <param name="principal">The network principal, when supplied by the transport.</param>
    /// <param name="hasTransport">Whether a network call exists even when its identity is missing.</param>
    /// <returns>A filter bound to this caller.</returns>
    Func<IEnumerable<ConnectedClient>, Task<IEnumerable<ConnectedClient>>> Capture(ClaimsPrincipal? principal = null, bool hasTransport = false);
}
