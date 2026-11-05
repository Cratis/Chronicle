// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Clients;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;

using KernelConnectedClient = Cratis.Chronicle.Concepts.Clients.ConnectedClient;
using KernelConnectionService = Cratis.Chronicle.Services.Clients.ConnectionService;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.given;

public class a_service_with_real_ownership : Specification
{
    protected const string Id = "the-connection";
    protected IConnectedClients _connectedClients;
    protected ConnectionOwnership _ownership;
    private protected KernelConnectionService _service;
    private protected OpenConnections _openConnections;
    protected SiloAddress _silo;
    protected TaskCompletionSource _hostsLookup;
    protected readonly TaskCompletionSource _hostsLookupRequested = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected IManagementGrain _management;
    readonly List<IDisposable> _subscriptions = [];

    void Establish()
    {
        _silo = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1);
        _connectedClients = Substitute.For<IConnectedClients>();
        _connectedClients.OnClientConnected(Arg.Any<ConnectionId>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(Task.CompletedTask);
        _connectedClients.OnClientPing(Arg.Any<ConnectionId>()).Returns(true);
        _connectedClients.IsConnected(Arg.Any<ConnectionId>()).Returns(false);
        _connectedClients.GetAllConnectedClients().Returns(Task.FromResult<IEnumerable<KernelConnectedClient>>([]));

        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IConnectedClients>(_silo.ToParsableString(), Arg.Any<string>()).Returns(_connectedClients);
        AllowingConnectionOwnership.WithAllowingReservations(grainFactory);
        _management = Substitute.For<IManagementGrain>();
        _management.GetHosts(true).Returns(_ => Hosts());
        grainFactory.GetGrain<IManagementGrain>(Arg.Any<long>(), Arg.Any<string>()).Returns(_management);
        var localSilo = Substitute.For<ILocalSiloDetails>();
        localSilo.SiloAddress.Returns(_silo);

        var options = Options.Create(new ChronicleOptions { ConnectedClients = new() { KeepAliveIntervalSeconds = 1 } });
        _ownership = new();
        var ownership = new ReleaseSignallingOwnership(_ownership, () => _released.TrySetResult());
        _openConnections = new();
        _service = new(
            grainFactory,
            localSilo,
            new ConnectedClientsQuery(grainFactory, options),
            _openConnections,
            ownership,
            new ConnectedClientsVisibility(options, ownership, new Microsoft.AspNetCore.Http.HttpContextAccessor()),
            NullLogger<KernelConnectionService>.Instance,
            options);
    }

    protected int Calls(string method) => _connectedClients.ReceivedCalls().Count(call => call.GetMethodInfo().Name == method);

    protected bool Owns(string subject) => _ownership.IsOwnedBy(Id, AuthenticatedCalls.OwnerOf(subject));

    /// <summary>
    /// Opens a connection and waits until the server has either answered it or refused it.
    /// </summary>
    /// <param name="context">The call context.</param>
    /// <param name="id">The connection identifier.</param>
    /// <returns>The failure, or null when the server answered.</returns>
    protected async Task<Exception?> Open(CallContext context, string id = Id)
    {
        var outcome = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _subscriptions.Add(_service.Connect(new ConnectRequest { ConnectionId = id }, context).Subscribe(
            _ => outcome.TrySetResult(null),
            error => outcome.TrySetResult(error),
            () => outcome.TrySetResult(new InvalidOperationException("ended"))));
        return await outcome.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
    }

    protected async Task<Exception?> KeepAlive(CallContext context, string id = Id) =>
        await Catch.Exception(() => _service.ConnectionKeepAlive(new ConnectionKeepAlive { ConnectionId = id }, context));

    /// <summary>
    /// Waits until the service has released a connection claim.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected Task ClaimReleased() => _released.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);

    void Destroy() => _subscriptions.ForEach(_ => _.Dispose());

    async Task<Dictionary<SiloAddress, SiloStatus>> Hosts()
    {
        _hostsLookupRequested.TrySetResult();
        if (_hostsLookup is not null) await _hostsLookup.Task;
        return new() { [_silo] = SiloStatus.Active };
    }

    sealed class ReleaseSignallingOwnership(IConnectionOwnership inner, Action released) : IConnectionOwnership
    {
        public ConnectionClaim Claim(ConnectionId connectionId, ConnectionOwner owner) => inner.Claim(connectionId, owner);

        public void Release(ConnectionId connectionId, ConnectionOwner owner)
        {
            inner.Release(connectionId, owner);
            released();
        }

        public bool IsOwnedBy(string connectionId, ConnectionOwner owner) => inner.IsOwnedBy(connectionId, owner);
    }
}
