// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Aksio.Cratis.Kernel.Contracts.Clients;
using Aksio.Cratis.Kernel.Grains.Clients;
using ProtoBuf.Grpc;

namespace Aksio.Cratis.Kernel.Services.Clients;

/// <summary>
/// Represents an implementation of <see cref="IConnectionService"/>.
/// </summary>
public class ConnectionService : IConnectionService
{
    readonly IGrainFactory _grainFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionService"/> class.
    /// </summary>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> to get grains with.</param>
    public ConnectionService(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    /// <inheritdoc/>
    public IObservable<ConnectionKeepAlive> Connect(
        ConnectRequest request,
        CallContext context = default)
    {
        var connectedClients = _grainFactory.GetGrain<IConnectedClients>(0);
        connectedClients.OnClientConnected(
            request.ConnectionId,
            request.ClientVersion,
            request.IsRunningWithDebugger).GetAwaiter().GetResult();

        var subject = new Subject<ConnectionKeepAlive>();

        _ = Task.Run(async () =>
        {
            while (!context.CancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);

                if (context.CancellationToken.IsCancellationRequested)
                {
                    break;
                }

                subject.OnNext(new ConnectionKeepAlive
                {
                    ConnectionId = request.ConnectionId
                });
            }

            await connectedClients.OnClientDisconnected(request.ConnectionId, "Client disconnected");
        });

        return subject;
    }

    /// <inheritdoc/>
    public void ConnectionKeepAlive(ConnectionKeepAlive keepAlive)
    {
        var connectedClients = _grainFactory.GetGrain<IConnectedClients>(0);
        connectedClients.OnClientPing(keepAlive.ConnectionId).GetAwaiter().GetResult();
    }
}
