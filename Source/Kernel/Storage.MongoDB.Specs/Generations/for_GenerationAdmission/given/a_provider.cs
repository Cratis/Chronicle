// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Orleans.Storage.MongoDB.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.for_GenerationAdmission.given;

public class a_provider(ReplicaSetMongoDBFixture fixture) : Specification
{
    protected readonly string _prefix = $"admit_{Guid.NewGuid():N}_";
    protected readonly EventStoreName _name = new("orders");
    protected MongoClient _client;
    protected Database _database;
    protected Storage _storage;
    protected MongoDBKernelStateResetHandler _reset;
    protected IMongoCollection<EventStore> _registry;

    void Establish()
    {
        _client = new(fixture.ConnectionString);
        var manager = Substitute.For<IMongoDBClientManager>();
        manager.GetClientFor(Arg.Any<MongoClientSettings>()).Returns(_client);
        var mongoOptions = Options.Create(new MongoDBOptions { Server = fixture.ConnectionString });
        var names = Options.Create(new MongoDBStorageOptions { DatabaseNamePrefix = _prefix });
        var options = Options.Create(new ChronicleOptions { Storage = new() { Type = "mongodb" } });
        _database = new(manager, mongoOptions, Substitute.For<ICustomSerializers>(), names);
        var factories = Substitute.For<IInstancesOf<ISinkFactory>>();
        factories.GetEnumerator().Returns(_ => Enumerable.Empty<ISinkFactory>().GetEnumerator());
        _storage = new(_database, Substitute.For<Json.IExpandoObjectConverter>(), new JsonSerializerOptions(), factories, Substitute.For<Cratis.Orleans.Storage.IJobsStorage>(), options, NullLoggerFactory.Instance);
        _reset = new(options, mongoOptions, manager, names);
        _registry = _database.GetCollection<EventStore>();
    }

    protected async Task Advance(uint protocol)
    {
        await _registry.InsertOneAsync(new(_name));
        var floor = new GenerationFloor(new(protocol), new(protocol), [GenerationCapability.FloorAdmission], new(Guid.NewGuid()), 0, default, "", "");
        var provider = new Generations.GenerationFloorStorage(_database.GetEventStoreDatabase(_name).GetCollection<BsonDocument>("generation-floor"));
        await provider.Advance(new(floor, 0, "spec", "floor", new(true, "uniform")));
    }

    protected async Task<string[]> Databases()
    {
        using var names = await _client.ListDatabaseNamesAsync();
        return (await names.ToListAsync()).Where(name => name.StartsWith(_prefix, StringComparison.Ordinal)).Order(StringComparer.Ordinal).ToArray();
    }

    async Task Destroy()
    {
        foreach (var name in await Databases())
        {
            await _client.DropDatabaseAsync(name);
        }
    }
}
