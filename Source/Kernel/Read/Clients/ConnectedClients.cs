// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Applications.Queries;
using Aksio.Cratis.Clients;
using Aksio.Cratis.Kernel.Grains.Clients;
using Microsoft.AspNetCore.Mvc;

namespace Read.Clients;

/// <summary>
/// Represents the API for querying connected clients.
/// </summary>
[Route("/api/clients/{microserviceId}")]
public class ConnectedClients : Controller
{
    readonly IConnectedClientsState _connectedClients;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectedClients"/> class.
    /// </summary>
    /// <param name="connectedClients">The <see cref="IConnectedClientsState"/>.</param>
    public ConnectedClients(IConnectedClientsState connectedClients)
    {
        _connectedClients = connectedClients;
    }

    /// <summary>
    /// Get and observe all connected clients for a specific microservice.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> to observe for.</param>
    /// <returns>Client observable of a collection of <see cref="ConnectedClient"/>.</returns>
    [HttpGet]
    public Task<ClientObservable<IEnumerable<ConnectedClient>>> ConnectedClientsForMicroservice(
        [FromRoute] MicroserviceId microserviceId)
    {
        var clientObservable = new ClientObservable<IEnumerable<ConnectedClient>>();
        var observable = _connectedClients.GetAllForMicroservice(microserviceId);
        var subscription = observable.Subscribe(_ => clientObservable.OnNext(_));
        clientObservable.ClientDisconnected = () =>
        {
            subscription.Dispose();
            if (observable is IDisposable disposableObservable)
            {
                disposableObservable.Dispose();
            }
        };

        return Task.FromResult(clientObservable);
    }
}
