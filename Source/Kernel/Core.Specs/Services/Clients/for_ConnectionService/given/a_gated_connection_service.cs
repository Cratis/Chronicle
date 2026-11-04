// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Clients;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.given;

public class a_gated_connection_service : Specification
{
    protected readonly Chronicle.given.ObservedConnectionOwnership _ownership = new();
    protected readonly OpenConnections _openConnections = new();
    protected readonly IConnectedClients _clients = Substitute.For<IConnectedClients>();
    protected ConnectionService _service;
    readonly List<CancellationTokenSource> _cancellations = [];
    readonly List<IDisposable> _subscriptions = [];
    readonly List<Task<Exception?>> _completions = [];

    void Establish()
    {
        var silo = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1);
        var grains = Substitute.For<IGrainFactory>();
        grains.GetGrain<IConnectedClients>(silo.ToParsableString(), Arg.Any<string>()).Returns(_clients);
        var local = Substitute.For<ILocalSiloDetails>();
        local.SiloAddress.Returns(silo);
        var options = Options.Create(new ChronicleOptions());
        _service = new ConnectionService(grains, local, new ConnectedClientsQuery(grains, options), _openConnections,
            NullLogger<ConnectionService>.Instance, options, _ownership, new ConnectionOwnerResolver(options), new Chronicle.given.UnrestrictedConnectedClientsVisibility());
    }

    protected (Task Connected, Task<Exception?> Completed) Start(CancellationTokenSource cancellation)
    {
        _cancellations.Add(cancellation);
        var connected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _subscriptions.Add(_service.Connect(new ConnectRequest { ConnectionId = "connection" }, new CallContext(new CallOptions(cancellationToken: cancellation.Token)))
            .Subscribe(_ => connected.TrySetResult(), error => completed.TrySetResult(error), () => completed.TrySetResult(null)));
        _completions.Add(completed.Task);
        return (connected.Task, completed.Task);
    }

    async Task Destroy()
    {
        _ownership.Entering = null;
        foreach (var cancellation in _cancellations) cancellation.Cancel();
        await Task.WhenAll(_completions).WaitAsync(TimeSpan.FromSeconds(5));
        foreach (var subscription in _subscriptions) subscription.Dispose();
        foreach (var cancellation in _cancellations) cancellation.Dispose();
    }
}
