// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Occurrences;
using Cratis.Metrics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Clients.for_ConnectedClients.when_disconnecting;

public class and_lease_expiry_is_slow_and_fails : Specification
{
    readonly ConnectionId _connection = ConnectionId.New();
    readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _continued = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly IMeter<ConnectedClients> _meter = Substitute.For<IMeter<ConnectedClients>>();
    ConnectedClients _clients;
    bool _disconnectCompleted;
    bool _stillConnected;
    int _count;

    async Task Establish()
    {
        var storage = Substitute.For<IStorage>();
        var store = Substitute.For<IEventStoreStorage>();
        var application = Substitute.For<IOccurrencesStorage>();
        var tenant = Substitute.For<IOccurrencesStorage>();
        storage.GetEventStores().Returns(Task.FromResult<IEnumerable<EventStoreName>>([new("store")]));
        storage.GetEventStore(new("store")).Returns(store);
        store.Occurrences.Returns(application);
        store.Namespaces.GetAll().Returns(Task.FromResult<IEnumerable<NamespaceState>>([new(new("tenant"), DateTimeOffset.UnixEpoch)]));
        store.GetNamespace(new("tenant")).Occurrences.Returns(tenant);
        application.ExpireLeases(Arg.Any<string>(), Arg.Any<DateTimeOffset>()).Returns(async _ =>
        {
            _started.SetResult();
            await _release.Task;
            throw new IOException("Simulated expiry failure");
        });
        tenant.ExpireLeases(Arg.Any<string>(), Arg.Any<DateTimeOffset>()).Returns(_ =>
        {
            _continued.SetResult();
            return Task.CompletedTask;
        });
        _clients = new(NullLogger<ConnectedClients>.Instance, _meter, Options.Create(new ChronicleOptions()), storage);
        await _clients.OnClientConnected(_connection, "1.0", false, 1, "app", "host", ".NET");
        _meter.ClearReceivedCalls();
    }

    async Task Because()
    {
        var disconnect = _clients.OnClientDisconnected(_connection, "closed");
        await _started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _disconnectCompleted = disconnect.IsCompletedSuccessfully;
        _stillConnected = await _clients.IsConnected(_connection);
        _count = await _clients.GetConnectionCount();
        _release.SetResult();
        await disconnect.WaitAsync(TimeSpan.FromSeconds(5));
        await _continued.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_finish_disconnect_without_waiting_for_storage() => _disconnectCompleted.ShouldBeTrue();
    [Fact] void should_remove_the_dead_connection_before_expiry_finishes() => _stillConnected.ShouldBeFalse();
    [Fact] void should_remove_the_dead_connection_from_the_count() => _count.ShouldEqual(0);
    [Fact] void should_update_the_meter_even_though_expiry_fails() => _meter.Received(1).ConnectedClients(0);
    [Fact] void should_continue_expiring_the_namespace_after_application_expiry_fails() => _continued.Task.IsCompletedSuccessfully.ShouldBeTrue();
}
