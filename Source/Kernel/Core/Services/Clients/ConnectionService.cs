// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Contracts.Clients;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Clients;

/// <summary>
/// Represents an implementation of <see cref="IConnectionService"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> to get grains with.</param>
/// <param name="connectionManager"><see cref="IClientConnectionManager"/> for tracking and controlling client connections.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
internal sealed class ConnectionService(
    IGrainFactory grainFactory,
    IClientConnectionManager connectionManager,
    ILogger<ConnectionService> logger) : IConnectionService
{
    /// <inheritdoc/>
    public IObservable<ConnectionKeepAlive> Connect(
        ConnectRequest request,
        CallContext context = default)
    {
        var subject = new Subject<ConnectionKeepAlive>();
        var connectedClients = grainFactory.GetGrain<IConnectedClients>(0);

        _ = Task.Run(
            async () =>
            {
                // Wait until the server is accepting connections (blocks during reset).
                await connectionManager.WaitUntilAcceptingConnections();

                // Create a linked CTS so the server can force-disconnect this client.
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
                connectionManager.Register(request.ConnectionId, linkedCts);

                try
                {
                    await connectedClients.OnClientConnected(
                        request.ConnectionId,
                        request.ClientVersion,
                        request.IsRunningWithDebugger);

                    while (!linkedCts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), linkedCts.Token).ConfigureAwait(false);

                        subject.OnNext(new ConnectionKeepAlive
                        {
                            ConnectionId = request.ConnectionId
                        });
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when the connection is terminated — either by client disconnect
                    // or server-initiated disconnection during a reset.
                }
                catch (Exception ex)
                {
                    logger.FailureDuringKeepAlive(request.ConnectionId, ex);
                }
                finally
                {
                    connectionManager.Unregister(request.ConnectionId);
                    subject.OnCompleted();
                    subject.Dispose();
                    await connectedClients.OnClientDisconnected(request.ConnectionId, "Client disconnected");
                }
            },
            CancellationToken.None);

        return subject;
    }

    /// <inheritdoc/>
    public async Task ConnectionKeepAlive(ConnectionKeepAlive keepAlive)
    {
        var connectedClients = grainFactory.GetGrain<IConnectedClients>(0);
        await connectedClients.OnClientPing(keepAlive.ConnectionId);
    }
}
