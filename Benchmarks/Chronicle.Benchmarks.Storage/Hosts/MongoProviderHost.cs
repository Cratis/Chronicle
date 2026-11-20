// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc;
using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.Chronicle.Storage.MongoDB.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Chronicle.Benchmarks.Storage.Hosts;

/// <summary>
/// Hosts a single-node MongoDB replica set and builds the real Chronicle MongoDB storage on it, with driver command
/// monitoring counting every command.
/// </summary>
internal sealed class MongoProviderHost : IStorageProviderHost
{
    const string Image = "mongo:8.2";
    const int Port = 27017;
    const string StartupCommand = "mongod --replSet rs0 --bind_ip_all > /proc/1/fd/1 2>/proc/1/fd/2 & " +
        "until mongosh --quiet --eval 'db.adminCommand(\"ping\")' >/dev/null 2>&1; do sleep 0.1; done; " +
        "mongosh --eval 'const result = rs.initiate({_id:\"rs0\",members:[{_id:0,host:\"localhost:27017\"}]}); " +
        "if (result.ok !== 1) { throw new Error(\"rs.initiate returned: \" + JSON.stringify(result)); }'; " +
        "tail -f /dev/null";
    static readonly Lazy<bool> _serialization = new(RegisterSerialization);
    IContainer? _container;
    IMongoClient _client = null!;
    string _connectionString = string.Empty;

    /// <inheritdoc/>
    public StorageProvider Provider => StorageProvider.Mongo;

    /// <inheritdoc/>
    public CommandCounter Counter { get; } = new();

    /// <inheritdoc/>
    public async Task Start()
    {
        _ = _serialization.Value;
        _container = new ContainerBuilder(Image)
            .WithEnvironment("GLIBC_TUNABLES", "glibc.pthread.rseq=1")
            .WithCommand("/bin/sh", "-c", StartupCommand)
            .WithPortBinding(Port, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(Port))
            .Build();
        await _container.StartAsync();
        _connectionString = $"mongodb://localhost:{_container.GetMappedPublicPort(Port)}/?directConnection=true";
        _client = new CountingClientManager(Counter).GetClientFor(MongoClientSettings.FromConnectionString(_connectionString));
        await WaitForPrimary();
    }

    /// <inheritdoc/>
    public IEventSequenceStorage CreateSequence()
    {
        var namespaceDatabase = CreateNamespaceDatabase(NewNamespace());
        var eventTypes = BenchmarkEvents.CreateEventTypes();
        var identities = BenchmarkEvents.CreateIdentities();
        var converter = new Json.ExpandoObjectConverter(new TypeFormats());
        var storage = new EventSequenceStorage(
            "bench-store",
            "bench-namespace",
            EventSequenceId.Log,
            namespaceDatabase,
            new EventConverter("bench-store", "bench-namespace", eventTypes, identities, converter),
            eventTypes,
            identities,
            converter,
            new JsonSerializerOptions(),
            NullLogger<EventSequenceStorage>.Instance);
        storage.EnsureIndexes().GetAwaiter().GetResult();
        return storage;
    }

    /// <inheritdoc/>
    public Task<(IUniqueConstraintsStorage Storage, UniqueConstraintDefinition Definition)> CreateConstraints()
    {
        var namespaceDatabase = CreateNamespaceDatabase(NewNamespace());
        var definition = new UniqueConstraintDefinition($"unique-{Guid.NewGuid():N}", [new UniqueConstraintEventDefinition(BenchmarkEvents.Selected.Id, ["value"])]);
        IUniqueConstraintsStorage storage = new UniqueConstraintsStorage(namespaceDatabase, EventSequenceId.Log, NullLogger<UniqueConstraintsStorage>.Instance);
        return Task.FromResult((storage, definition));
    }

    /// <inheritdoc/>
    public async Task<SettingsLedger> BuildLedger(StorageRunOptions options, LoadReading loadAtStart)
    {
        var admin = _client.GetDatabase("admin");
        var build = await admin.RunCommandAsync<BsonDocument>(new BsonDocument("buildInfo", 1));
        var defaults = await admin.RunCommandAsync<BsonDocument>(new BsonDocument("getDefaultRWConcern", 1));
        var config = await admin.RunCommandAsync<BsonDocument>(new BsonDocument("replSetGetConfig", 1));
        var writeConcern = defaults.GetValue("defaultWriteConcern", new BsonDocument()).AsBsonDocument;
        var concern = writeConcern.Contains("w") ? writeConcern["w"].ToString() : null;
        bool? journal = writeConcern.Contains("j") ? writeConcern["j"].ToBoolean() : null;
        var majorityJournal = config["config"].AsBsonDocument.GetValue("writeConcernMajorityJournalDefault", true).ToBoolean();
        var durability = new Dictionary<string, string>
        {
            ["defaultWriteConcern.w"] = concern ?? "majority (implicit)",
            ["defaultWriteConcern.j"] = journal?.ToString() ?? "server decides",
            ["writeConcernMajorityJournalDefault"] = majorityJournal.ToString()
        };
        return HostFacts.Ledger(
            StorageProvider.Mongo,
            options,
            loadAtStart,
            Image,
            build["version"].AsString,
            durability,
            DurabilityClassifier.ForMongo(concern, journal, majorityJournal),
            100,
            HostFacts.ImageDigest(Image));
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();
        if (_container is not null) await _container.DisposeAsync();
    }

    static bool RegisterSerialization()
    {
        var services = new ServiceCollection();
        services.AddCratisArcCore();
        services.AddCratisMongoDB(configureMongoDB: mongoDB => mongoDB.WithCamelCaseNamingPolicy());
        BsonSerializer.RegisterSerializationProvider(new ConstraintDefinitionSerializationProvider());
        return true;
    }

    static string NewNamespace() => $"ns{Guid.NewGuid():N}";

    EventStoreNamespaceDatabase CreateNamespaceDatabase(string namespaceName) =>
        new("bench-store", namespaceName, new CountingClientManager(Counter), Options.Create(new MongoDBOptions { Server = _connectionString, DirectConnection = true }));

    async Task WaitForPrimary()
    {
        var deadline = DateTime.UtcNow.AddSeconds(45);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var hello = await _client.GetDatabase("admin").RunCommandAsync<BsonDocument>(new BsonDocument("hello", 1));
                if (hello.GetValue("isWritablePrimary", false).ToBoolean()) return;
            }
            catch (MongoException)
            {
                // The node is still starting.
            }

            await Task.Delay(250);
        }

        throw new TimeoutException("MongoDB did not become primary within 45 seconds.");
    }
}
