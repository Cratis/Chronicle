// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Clients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Reflection;
using ProtoBuf.Meta;

namespace Cratis.Chronicle.Services.Clients;

/// <summary>
/// Represents an implementation of <see cref="IConnectionService"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> to get grains with.</param>
/// <param name="localSiloDetails"><see cref="ILocalSiloDetails"/> for the silo terminating the client connections.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
/// <param name="options"><see cref="IOptions{ChronicleOptions}"/> for configuration.</param>
internal sealed class ConnectionService(
    IGrainFactory grainFactory,
    ILocalSiloDetails localSiloDetails,
    ILogger<ConnectionService> logger,
    IOptions<ChronicleOptions> options) : IConnectionService
{
    static readonly Lazy<string> _schemaDefinition = new(GenerateSchema);
    readonly TimeSpan _observeConnectedClientsInterval = TimeSpan.FromSeconds(options.Value.ConnectedClients.ObserveIntervalSeconds);
    readonly TimeSpan _keepAliveInterval = TimeSpan.FromSeconds(options.Value.ConnectedClients.KeepAliveIntervalSeconds);

    /// <inheritdoc/>
    public IObservable<ConnectionKeepAlive> Connect(
        ConnectRequest request,
        CallContext context = default)
    {
        // Replaying, so the first keep-alive is not lost. It is sent as soon as the client is registered, which
        // races the transport subscribing to this subject - and a plain Subject drops what it has no subscriber
        // for, which would silently put the client back to waiting a full interval. One is all that needs
        // replaying: a keep-alive carries nothing but the connection it belongs to.
        var subject = new ReplaySubject<ConnectionKeepAlive>(1);
        var connectedClients = grainFactory.GetConnectedClients(localSiloDetails.SiloAddress);

        _ = Task.Run(
            async () =>
            {
                await connectedClients.OnClientConnected(
                    request.ConnectionId,
                    request.ClientVersion,
                    request.IsRunningWithDebugger,
                    request.ProcessId,
                    request.ProcessPath,
                    request.MachineName,
                    request.ClientType);

                try
                {
                    // The session is established the moment the client is registered, and the first keep-alive is
                    // how the client learns that - it does not treat itself as connected until one arrives. Send it
                    // straight away rather than after a first interval, or every client everywhere pays the whole
                    // keep-alive interval before it can do anything.
                    subject.OnNext(new ConnectionKeepAlive
                    {
                        ConnectionId = request.ConnectionId
                    });

                    while (!context.CancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(_keepAliveInterval).ConfigureAwait(false);

                        if (context.CancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        subject.OnNext(new ConnectionKeepAlive
                        {
                            ConnectionId = request.ConnectionId
                        });
                    }
                }
                catch (Exception ex)
                {
                    logger.FailureDuringKeepAlive(request.ConnectionId, ex);
                }

                await connectedClients.OnClientDisconnected(request.ConnectionId, "Client disconnected");
            },
            context.CancellationToken);

        context.CancellationToken.Register(() =>
        {
            subject.OnCompleted();
            subject.Dispose();
        });

        return subject;
    }

    /// <inheritdoc/>
    public async Task ConnectionKeepAlive(ConnectionKeepAlive keepAlive)
    {
        var connectedClients = grainFactory.GetConnectedClients(localSiloDetails.SiloAddress);
        await connectedClients.OnClientPing(keepAlive.ConnectionId);
    }

    /// <inheritdoc/>
    [AllowAnonymous]
    public Task<DescriptorSetResponse> GetDescriptorSet()
    {
        return Task.FromResult(new DescriptorSetResponse
        {
            SchemaDefinition = _schemaDefinition.Value
        });
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The per-silo lookups are issued together, so a faulting silo no longer aborts the sweep before the remaining
    /// silos are asked - every silo is queried, and the first fault surfaces once they have all settled rather than
    /// immediately. The observable outcome is unchanged: the same exception type still propagates, and a fault still
    /// fails the whole call rather than returning a partial cluster view.
    /// </remarks>
    public async Task<IEnumerable<ConnectedClient>> GetConnectedClients(CallContext context = default)
    {
        var management = grainFactory.GetGrain<IManagementGrain>(0);
        var hosts = await management.GetHosts(onlyActive: true);
        var clientsPerSilo = await Task.WhenAll(hosts.Keys.Select(GetConnectedClientsForSilo));
        return clientsPerSilo.SelectMany(clients => clients).ToList();
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<ConnectedClient>> ObserveConnectedClients(CallContext context = default)
    {
        var subject = new Subject<IEnumerable<ConnectedClient>>();
        var subscription = Observable
            .Timer(TimeSpan.Zero, _observeConnectedClientsInterval)
            .SelectMany(_ => Observable.FromAsync(() => GetConnectedClients(context)))
            .DistinctUntilChanged(ConnectedClientsComparer.Instance)
            .Subscribe(subject);

        context.CancellationToken.Register(() =>
        {
            subscription.Dispose();
            subject.OnCompleted();
        });

        return subject;
    }

    static string GenerateSchema()
    {
        var generator = new SchemaGenerator
        {
            ProtoSyntax = ProtoSyntax.Proto3
        };

        // SchemaGenerator requires all types in a single call to share the same proto package
        // (derived from C# namespace). Group by namespace and concatenate the resulting schemas.
        var schemas = Contracts.AvailableServices.All
            .GroupBy(t => t.Namespace ?? string.Empty)
            .Select(group => generator.GetSchema(group.ToArray()));

        return string.Join('\n', schemas);
    }

    async Task<IEnumerable<ConnectedClient>> GetConnectedClientsForSilo(SiloAddress silo)
    {
        var connectedClients = await grainFactory.GetConnectedClients(silo).GetAllConnectedClients();
        return connectedClients.Select(client => new ConnectedClient
        {
            ConnectionId = client.ConnectionId,
            Version = client.Version,
            LastSeen = client.LastSeen,
            IsRunningWithDebugger = client.IsRunningWithDebugger,
            SiloAddress = silo.ToParsableString(),
            ProcessId = client.ProcessId,
            ProcessPath = client.ProcessPath,
            MachineName = client.MachineName,
            ClientType = client.ClientType
        });
    }

    sealed class ConnectedClientsComparer : IEqualityComparer<IEnumerable<ConnectedClient>>
    {
        public static readonly ConnectedClientsComparer Instance = new();

        public bool Equals(IEnumerable<ConnectedClient>? x, IEnumerable<ConnectedClient>? y)
        {
            if (x is null || y is null)
            {
                return ReferenceEquals(x, y);
            }

            return x.Select(Identity).Order().SequenceEqual(y.Select(Identity).Order());
        }

        public int GetHashCode(IEnumerable<ConnectedClient> obj) => 0;

        /// <summary>
        /// Gets the identity of a client, covering every value observers display - including
        /// LastSeen, so a watching Workbench sees the last-seen time tick while a client is
        /// connected. The comparer's suppression then only kicks in when nothing at all changed,
        /// which in practice means no clients are connected.
        /// </summary>
        /// <param name="client">The <see cref="ConnectedClient"/> to get the identity for.</param>
        /// <returns>A string identifying the client.</returns>
        static string Identity(ConnectedClient client) => $"{client.SiloAddress}|{client.ConnectionId}|{client.Version}|{client.IsRunningWithDebugger}|{client.LastSeen}|{client.ProcessId}|{client.ProcessPath}|{client.MachineName}|{client.ClientType}";
    }
}
