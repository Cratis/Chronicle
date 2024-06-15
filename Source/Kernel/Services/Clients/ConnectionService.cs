// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Kernel.Contracts.Clients;
using Cratis.Kernel.Grains.Clients;
using ProtoBuf.Grpc;

namespace Cratis.Kernel.Services.Clients;

/// <summary>
/// Represents an implementation of <see cref="IConnectionService"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ConnectionService"/> class.
/// </remarks>
/// <param name="grainFactory"><see cref="IGrainFactory"/> to get grains with.</param>
public class ConnectionService(IGrainFactory grainFactory) : IConnectionService
{
    /// <inheritdoc/>
    public IObservable<ConnectionKeepAlive> Connect(
        ConnectRequest request,
        CallContext context = default)
    {
        var connectedClients = grainFactory.GetGrain<IConnectedClients>(0);
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
    public async Task ConnectionKeepAlive(ConnectionKeepAlive keepAlive)
    {
        var connectedClients = grainFactory.GetGrain<IConnectedClients>(0);
        await connectedClients.OnClientPing(keepAlive.ConnectionId);
    }
}
