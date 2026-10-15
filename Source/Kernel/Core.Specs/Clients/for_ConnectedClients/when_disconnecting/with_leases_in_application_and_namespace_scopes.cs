// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Occurrences;
using Cratis.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Clients.for_ConnectedClients.when_disconnecting;

public class with_leases_in_application_and_namespace_scopes : Specification
{
    readonly ConnectionId _connection = ConnectionId.New();
    readonly IOccurrencesStorage _application = Substitute.For<IOccurrencesStorage>();
    readonly IOccurrencesStorage _tenant = Substitute.For<IOccurrencesStorage>();
    readonly TaskCompletionSource _expired = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ConnectedClients _clients;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        var store = Substitute.For<IEventStoreStorage>();
        var tenant = Substitute.For<IEventStoreNamespaceStorage>();
        storage.GetEventStores().Returns(Task.FromResult<IEnumerable<EventStoreName>>([new("store")]));
        storage.GetEventStore(new("store")).Returns(store);
        store.Occurrences.Returns(_application);
        store.Namespaces.GetAll().Returns(Task.FromResult<IEnumerable<NamespaceState>>([new(new("tenant"), DateTimeOffset.UnixEpoch)]));
        store.GetNamespace(new("tenant")).Returns(tenant);
        tenant.Occurrences.Returns(_tenant);
        _tenant.ExpireLeases(Arg.Any<string>(), Arg.Any<DateTimeOffset>()).Returns(_ =>
        {
            _expired.SetResult();
            return Task.CompletedTask;
        });
        _clients = new(Substitute.For<ILogger<ConnectedClients>>(), Substitute.For<IMeter<ConnectedClients>>(), Options.Create(new ChronicleOptions()), storage);
    }

    async Task Because()
    {
        await _clients.OnClientDisconnected(_connection, "closed");
        await _expired.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }
    [Fact] async Task should_expire_application_leases() => await _application.Received(1).ExpireLeases(_connection.Value, Arg.Any<DateTimeOffset>());
    [Fact] async Task should_expire_namespace_leases_without_event_data() => await _tenant.Received(1).ExpireLeases(_connection.Value, Arg.Any<DateTimeOffset>());
}
