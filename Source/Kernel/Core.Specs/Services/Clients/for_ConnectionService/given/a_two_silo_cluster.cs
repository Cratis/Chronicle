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
    protected ConnectionReservation _grain;
    protected IConnectionReservation _reservation;
    protected SiloAddress _siloA;
    protected SiloAddress _siloB;
    protected ConnectionOwnership _ownershipA;
    protected ConnectionOwnership _ownershipB;
    private protected KernelConnectionService _serviceA;
    private protected KernelConnectionService _serviceB;
    readonly TaskCompletionSource<Exception?> _terminated = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly List<IDisposable> _subscriptions = [];
    readonly List<(int Count, TaskCompletionSource Reached)> _waiting = [];
    int _keepAlives;

    void Establish()
    {
        _clock = new();
        _siloA = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1);
        _siloB = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11112), 2);
        var membership = Substitute.For<IClusterMembershipService>();
        membership.CurrentSnapshot.Returns(new ClusterMembershipSnapshot(
            new[] { _siloA, _siloB }.ToImmutableDictionary(silo => silo, silo => new ClusterMember(silo, SiloStatus.Active, silo.ToString())),
            new MembershipVersion(1)));
        _grain = new SharedReservation(membership) { Clock = _clock };
        _reservation = new SignallingReservation(Decorate(new TurnBasedReservation(_grain)), _released);

        var options = Options.Create(new ChronicleOptions { ConnectedClients = new() { KeepAliveIntervalSeconds = 1 }, Clustering = new() { ResponseTimeout = ResponseTimeout } });
        (_serviceA, _ownershipA) = CreateService(_siloA, options);
        (_serviceB, _ownershipB) = CreateService(_siloB, options);
    }

    /// <summary>
    /// Gets how long a silo waits for the reservation grain.
    /// </summary>
    protected virtual TimeSpan ResponseTimeout => TimeSpan.FromMilliseconds(300);

    /// <summary>
    /// Gets the number of keep-alives received so far on the opened stream.
    /// </summary>
    protected int KeepAlivesReceived => Volatile.Read(ref _keepAlives);

    /// <summary>
    /// Waits until the opened stream has received the given number of keep-alives in total.
    /// </summary>
    /// <param name="count">The total number of keep-alives.</param>
    /// <returns>An awaitable task.</returns>
    protected Task KeepAlivesReached(int count)
    {
        var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_waiting)
        {
            if (KeepAlivesReceived >= count) reached.TrySetResult();
            else _waiting.Add((count, reached));
        }

        return reached.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
    }

    /// <summary>
    /// Gives a spec the chance to put something between the silos and the reservation grain they share.
    /// </summary>
    /// <param name="grain">The reservation grain.</param>
    /// <returns>The <see cref="IConnectionReservation"/> the silos use.</returns>
    protected virtual IConnectionReservation Decorate(IConnectionReservation grain) => grain;

    protected bool Owns(ConnectionOwnership ownership, string subject) => ownership.IsOwnedBy(Id, AuthenticatedCalls.OwnerOf(subject));

    private protected async Task<Exception?> Open(KernelConnectionService service, CallContext context)
    {
        var outcome = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _subscriptions.Add(service.Connect(new ConnectRequest { ConnectionId = Id }, context).Subscribe(
            _ =>
            {
                outcome.TrySetResult(null);
                lock (_waiting)
                {
                    var total = Interlocked.Increment(ref _keepAlives);
                    foreach (var (count, reached) in _waiting.Where(_ => total >= _.Count)) reached.TrySetResult();
                    _waiting.RemoveAll(_ => total >= _.Count);
                }
            },
            error =>
            {
                outcome.TrySetResult(error);
                _terminated.TrySetResult(error);
            },
            () =>
            {
                outcome.TrySetResult(new InvalidOperationException("ended"));
                _terminated.TrySetResult(null);
            }));
        return await outcome.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
    }

    /// <summary>
    /// Waits until the stream of a connection has ended.
    /// </summary>
    /// <returns>The error it ended with, or null when it completed.</returns>
    protected Task<Exception?> Terminated() => _terminated.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);

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

    /// <summary>
    /// Runs one call at a time against the shared grain, as Orleans does for a non-reentrant grain.
    /// </summary>
    /// <param name="inner">The grain the calls run against.</param>
    sealed class TurnBasedReservation(IConnectionReservation inner) : IConnectionReservation
    {
        readonly Lock _turn = new();

        public Task<bool> Claim(ConnectionOwner owner, SiloAddress silo, Guid token, TimeSpan lease)
        {
            lock (_turn)
            {
                return inner.Claim(owner, silo, token, lease);
            }
        }

        public Task Release(ConnectionOwner owner, Guid token)
        {
            lock (_turn)
            {
                return inner.Release(owner, token);
            }
        }
    }

    sealed class SignallingReservation(IConnectionReservation inner, TaskCompletionSource released) : IConnectionReservation
    {
        public Task<bool> Claim(ConnectionOwner owner, SiloAddress silo, Guid token, TimeSpan lease) => inner.Claim(owner, silo, token, lease);

        public async Task Release(ConnectionOwner owner, Guid token)
        {
            await inner.Release(owner, token);
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
