// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;
using ConnectedClient = Cratis.Kernel.Contracts.Clients.ConnectedClient;

namespace Cratis.API.Clients;

/// <summary>
/// Represents the API for querying connected clients.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ConnectedClients"/> class.
/// </remarks>
[Route("/api/clients")]
public class ConnectedClients() : Controller
{
    /// <summary>
    /// Get and observe all connected clients for a specific event store.
    /// </summary>
    /// <returns>Client observable of a collection of <see cref="ConnectedClient"/>.</returns>
    [HttpGet]
    public Task<IEnumerable<ConnectedClient>> AllConnectedClients()
    {
        throw new NotImplementedException();
    }
}
