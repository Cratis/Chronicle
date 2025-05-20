// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ConnectedClient = Cratis.Chronicle.Contracts.Clients.ConnectedClient;

namespace Cratis.Chronicle.Api.Clients;

/// <summary>
/// Represents the API for querying connected clients.
/// </summary>
[Route("/api/clients")]
public class ConnectedClients : Controller
{
    /// <summary>
    /// Get and observe all connected clients for a specific event store.
    /// </summary>
    /// <returns>Client observable of a collection of <see cref="ConnectedClient"/>.</returns>
    /// <exception cref="NotImplementedException">Not implemented.</exception>
    [HttpGet]
    public Task<IEnumerable<ConnectedClient>> AllConnectedClients()
    {
        throw new NotImplementedException();
    }
}
