// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Applications.Queries;
using Cratis.Connections;
using Cratis.Kernel.Grains.Clients;
using Microsoft.AspNetCore.Mvc;

namespace Read.Clients;

/// <summary>
/// Represents the API for querying connected clients.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ConnectedClients"/> class.
/// </remarks>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
[Route("/api/clients")]
public class ConnectedClients(IGrainFactory grainFactory) : Controller
{
    /// <summary>
    /// Get and observe all connected clients for a specific microservice.
    /// </summary>
    /// <returns>Client observable of a collection of <see cref="ConnectedClient"/>.</returns>
    [HttpGet]
    public ClientObservable<IEnumerable<ConnectedClient>> AllConnectedClients()
    {
        var clientObservable = new ClientObservable<IEnumerable<ConnectedClient>>();
        var connectedClients = grainFactory.GetGrain<IConnectedClients>(0);
        #pragma warning disable CA2000 // Call System.IDisposable.Dispose on object created by 'new CancellationTokenSource()' - it is disposed when the client disconnects
        var cancellationTokenSource = new CancellationTokenSource();

        _ = Task.Run(
            async () =>
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    var clients = await connectedClients.GetAllConnectedClients();
                    clientObservable.OnNext(clients);
                    await Task.Delay(5000);
                }
            },
            cancellationTokenSource.Token);

        clientObservable.ClientDisconnected = () =>
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
        };

        return clientObservable;
    }
}
