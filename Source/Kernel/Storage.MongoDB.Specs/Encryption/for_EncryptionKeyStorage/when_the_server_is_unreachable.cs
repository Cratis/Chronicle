// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.MongoDB;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage;

/// <summary>
/// A real driver against a port nobody listens on: server selection would hold each call for 30 seconds.
/// </summary>
public class when_the_server_is_unreachable : Specification
{
    static readonly TimeSpan _fenceTimeout = TimeSpan.FromMilliseconds(200);
    MongoClient _client;
    EncryptionKeyStorage _storage;
    Exception _read;
    Exception _confirm;
    TimeSpan _elapsed;

    void Establish()
    {
        _client = new(new MongoClientSettings
        {
            Server = new MongoServerAddress("127.0.0.1", UnusedPort()),
            ServerSelectionTimeout = TimeSpan.FromSeconds(30),
            DirectConnection = true
        });
        var server = _client.GetDatabase("unreachable");
        var database = Substitute.For<IDatabase>();
        var eventStore = Substitute.For<IEventStoreDatabase>();
        var namespaceDatabase = Substitute.For<IEventStoreNamespaceDatabase>();
        database.GetEventStoreDatabase(EventStoreName.NotSet).Returns(eventStore);
        eventStore.GetNamespaceDatabase(EventStoreNamespaceName.NotSet).Returns(namespaceDatabase);
        namespaceDatabase.GetCollection<EncryptionKeyForIdentifier>(Arg.Any<string>()).Returns(server.GetCollection<EncryptionKeyForIdentifier>("encryption-keys"));
        namespaceDatabase.GetCollection<EncryptionKeyErasureForIdentifier>(Arg.Any<string>()).Returns(server.GetCollection<EncryptionKeyErasureForIdentifier>("encryption-key-erasures"));
        _storage = new(database, Substitute.For<IMongoDBTopology>(), new EncryptionKeyErasureOptions { FenceTimeout = _fenceTimeout });
    }

    async Task Because()
    {
        var stopwatch = Stopwatch.StartNew();
        _read = await Catch.Exception(() => _storage.GetFenceFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier"));
        _confirm = await Catch.Exception(() => _storage.ConfirmErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier", "request"));
        _elapsed = stopwatch.Elapsed;
    }

    void Destroy() => _client.Dispose();

    [Fact] void should_report_the_fence_read_unavailable() => _read.ShouldBeOfExactType<EncryptionKeyFenceUnavailable>();
    [Fact] void should_report_the_confirmation_unavailable() => _confirm.ShouldBeOfExactType<EncryptionKeyFenceUnavailable>();
    [Fact] void should_give_up_long_before_server_selection_does() => (_elapsed < TimeSpan.FromSeconds(10)).ShouldBeTrue();

    static int UnusedPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
