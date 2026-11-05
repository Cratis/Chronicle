// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Net;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Clients;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;
using KernelConnectedClient = Cratis.Chronicle.Concepts.Clients.ConnectedClient;
using KernelConnectionService = Cratis.Chronicle.Services.Clients.ConnectionService;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.given;

/// <summary>
/// Two silos, each with its own <see cref="ConnectionService"/> and local ownership, sharing one reservation grain.
/// </summary>
public class a_two_silo_cluster : Specification
{
    protected const string Id = "the-connection";
    protected ManualClock _clock;
    protected IConnectionReservation _reservation;
    protected SiloAddress _siloA;
    protected SiloAddress _siloB;
    protected ConnectionOwnership _ownershipA;
    protected ConnectionOwnership _ownershipB;
    private protected KernelConnectionService _serviceA;
    private protected KernelConnectionService _serviceB;
    readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly List<IDisposable> _subscriptions = [];

    void Establish()
    {
        _clock = new();
        _siloA = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1);
        _siloB = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11112), 2);
        var membership = Substitute.For<IClusterMembershipService>();
        membership.CurrentSnapshot.Returns(new ClusterMembershipSnapshot(
            new[] { _siloA, _siloB }.ToImmutableDictionary(silo => silo, silo => new ClusterMember(silo, SiloStatus.Active, silo.ToString())),
            new MembershipVersion(1)));
        _reservation = new SignallingReservation(CreateReservation(membership), _released);

        var options = Options.Create(new ChronicleOptions { ConnectedClients = new() { KeepAliveIntervalSeconds = 1 }, Clustering = new() { ResponseTimeout = TimeSpan.FromMilliseconds(300) } });
        (_serviceA, _ownershipA) = CreateService(_siloA, options);
        (_serviceB, _ownershipB) = CreateService(_siloB, options);
    }

    /// <summary>
    /// Creates the reservation the silos share.
    /// </summary>
    /// <param name="membership">The <see cref="IClusterMembershipService"/>.</param>
    /// <returns>The <see cref="IConnectionReservation"/>.</returns>
    protected virtual IConnectionReservation CreateReservation(IClusterMembershipService membership) => new SharedReservation(membership) { Clock = _clock };

    protected bool Owns(ConnectionOwnership ownership, string subject) => ownership.IsOwnedBy(Id, AuthenticatedCalls.OwnerOf(subject));

    private protected async Task<Exception?> Open(KernelConnectionService service, CallContext context)
    {
        var outcome = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _subscriptions.Add(service.Connect(new ConnectRequest { ConnectionId = Id }, context).Subscribe(
            _ => outcome.TrySetResult(null),
            error => outcome.TrySetResult(error),
            () => outcome.TrySetResult(new InvalidOperationException("ended"))));
        return await outcome.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
    }

    /// <summary>
    /// Waits until the reservation has been released by a silo.
    /// </summary>
    /// <returns>An awaitable task.</returns>
    protected Task Released() => _released.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);

    void Destroy() => _subscriptions.ForEach(_ => _.Dispose());

    (KernelConnectionService Service, ConnectionOwnership Ownership) CreateService(SiloAddress silo, IOptions<ChronicleOptions> options)
    {
        var connectedClients = Substitute.For<IConnectedClients>();
        connectedClients.OnClientConnected(Arg.Any<ConnectionId>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(Task.CompletedTask);
        connectedClients.OnClientPing(Arg.Any<ConnectionId>()).Returns(true);
        connectedClients.IsConnected(Arg.Any<ConnectionId>()).Returns(false);
        connectedClients.GetAllConnectedClients().Returns(Task.FromResult<IEnumerable<KernelConnectedClient>>([]));

        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IConnectedClients>(Arg.Any<string>(), Arg.Any<string>()).Returns(connectedClients);
        grainFactory.GetGrain<IConnectionReservation>(Arg.Any<string>(), Arg.Any<string>()).Returns(_ => _reservation);
        var management = Substitute.For<IManagementGrain>();
        management.GetHosts(true).Returns(new Dictionary<SiloAddress, SiloStatus> { [_siloA] = SiloStatus.Active, [_siloB] = SiloStatus.Active });
        grainFactory.GetGrain<IManagementGrain>(Arg.Any<long>(), Arg.Any<string>()).Returns(management);
        var localSilo = Substitute.For<ILocalSiloDetails>();
        localSilo.SiloAddress.Returns(silo);

        var ownership = new ConnectionOwnership(_clock);
        var service = new KernelConnectionService(
            grainFactory,
            localSilo,
            new ConnectedClientsQuery(grainFactory, options),
            new OpenConnections(),
            ownership,
            new ConnectedClientsVisibility(options, ownership, new HttpContextAccessor()),
            NullLogger<KernelConnectionService>.Instance,
            options);
        return (service, ownership);
    }

    sealed class SignallingReservation(IConnectionReservation inner, TaskCompletionSource released) : IConnectionReservation
    {
        public Task<bool> Claim(ConnectionOwner owner, SiloAddress silo) => inner.Claim(owner, silo);

        public async Task Release(ConnectionOwner owner, SiloAddress silo)
        {
            await inner.Release(owner, silo);
            released.TrySetResult();
        }
    }

    protected sealed class SharedReservation(IClusterMembershipService membership) : ConnectionReservation(membership)
    {
        internal override void Keep(TimeSpan period)
        {
        }
    }

    protected sealed class ManualClock : TimeProvider
    {
        DateTimeOffset _now = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public void Advance(TimeSpan by) => _now += by;

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
