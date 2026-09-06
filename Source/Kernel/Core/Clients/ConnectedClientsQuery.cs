// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Clients;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Represents the cluster-wide view of which clients are connected.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> to get grains with.</param>
/// <param name="options"><see cref="IOptions{ChronicleOptions}"/> for configuration.</param>
/// <remarks>
/// A client connects to one silo, so no single silo knows the whole picture. Everything that wants the cluster view -
/// the gRPC connection service and the read model the Workbench queries - asks here rather than each assembling the
/// sweep itself.
/// </remarks>
public sealed class ConnectedClientsQuery(IGrainFactory grainFactory, IOptions<ChronicleOptions> options)
{
    readonly TimeSpan _observeInterval = TimeSpan.FromSeconds(options.Value.ConnectedClients.ObserveIntervalSeconds);

    /// <summary>
    /// Gets every client connected to the cluster, across all silos.
    /// </summary>
    /// <returns>A collection of <see cref="ConnectedClient"/>.</returns>
    /// <remarks>
    /// The per-silo lookups are issued together, so a faulting silo no longer aborts the sweep before the remaining
    /// silos are asked - every silo is queried, and the first fault surfaces once they have all settled rather than
    /// immediately. A fault still fails the whole call rather than returning a partial cluster view.
    /// </remarks>
    public async Task<IEnumerable<ConnectedClient>> GetAll()
    {
        var management = grainFactory.GetGrain<IManagementGrain>(0);
        var hosts = await management.GetHosts(onlyActive: true);
        var clientsPerSilo = await Task.WhenAll(hosts.Keys.Select(GetAllForSilo));
        return clientsPerSilo.SelectMany(clients => clients).ToList();
    }

    /// <summary>
    /// Observes every client connected to the cluster, across all silos.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for cancelling the observation.</param>
    /// <returns>An observable of collections of <see cref="ConnectedClient"/>.</returns>
    public IObservable<IEnumerable<ConnectedClient>> ObserveAll(CancellationToken cancellationToken)
    {
        var subject = new Subject<IEnumerable<ConnectedClient>>();
        var subscription = Observable
            .Timer(TimeSpan.Zero, _observeInterval)
            .SelectMany(_ => Observable.FromAsync(GetAll))
            .DistinctUntilChanged(ConnectedClientsComparer.Instance)
            .Subscribe(subject);

        cancellationToken.Register(() =>
        {
            subscription.Dispose();
            subject.OnCompleted();
        });

        return subject;
    }

    /// <summary>
    /// Gets the clients an observer's subscription delivers events to.
    /// </summary>
    /// <param name="eventStore">The event store the observer belongs to.</param>
    /// <param name="namespace">The namespace within the event store the observer belongs to.</param>
    /// <param name="observerId">The identifier of the observer.</param>
    /// <param name="eventSequenceId">The event sequence the observer observes.</param>
    /// <returns>A collection of <see cref="ConnectedClient"/>.</returns>
    /// <remarks>
    /// Only client-owned observers have connected clients - for kernel-owned observers this is empty.
    /// </remarks>
    public async Task<IEnumerable<ConnectedClient>> GetAllForObserver(
        string eventStore,
        string @namespace,
        string observerId,
        string eventSequenceId)
    {
        var sequenceId = string.IsNullOrEmpty(eventSequenceId) ? EventSequenceId.Log : (EventSequenceId)eventSequenceId;
        var key = new ObserverKey(observerId, eventStore, @namespace, sequenceId);
        var subscription = await grainFactory.GetGrain<Observation.IObserver>(key).GetSubscription();
        var clients = new List<ConnectedClient>();

        foreach (var target in subscription.Targets.Where(_ => _.ConnectedClient is not null))
        {
            // The target holds the client as it looked when it subscribed - resolve it from the
            // silo's connected-clients registry for a fresh LastSeen, falling back to the snapshot
            // if it disconnected between the subscription being read and the lookup.
            var client = target.ConnectedClient!;
            var connectedClients = grainFactory.GetConnectedClients(target.SiloAddress);

            if (await connectedClients.IsConnected(client.ConnectionId))
            {
                client = await connectedClients.GetConnectedClient(client.ConnectionId);
            }

            clients.Add(ToContract(client, target.SiloAddress));
        }

        return clients;
    }

    static ConnectedClient ToContract(Concepts.Clients.ConnectedClient client, SiloAddress silo) =>
        new()
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
        };

    async Task<IEnumerable<ConnectedClient>> GetAllForSilo(SiloAddress silo)
    {
        var connectedClients = await grainFactory.GetConnectedClients(silo).GetAllConnectedClients();
        return connectedClients.Select(client => ToContract(client, silo));
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
        /// <param name="client">The <see cref="ConnectedClient"/> to get the identity of.</param>
        /// <returns>The identity as a string.</returns>
        static string Identity(ConnectedClient client) =>
            $"{client.SiloAddress}/{client.ConnectionId}/{client.Version}/{client.LastSeen:O}/{client.IsRunningWithDebugger}/{client.ProcessId}/{client.ProcessPath}/{client.MachineName}/{client.ClientType}";
    }
}
