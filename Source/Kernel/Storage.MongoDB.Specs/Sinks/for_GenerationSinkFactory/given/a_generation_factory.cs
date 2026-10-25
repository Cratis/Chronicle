// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sinks.for_Sinks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_GenerationSinkFactory.given;

public class a_generation_factory : Specification
{
    protected SinkFactory _factory;
    protected GenerationSinkContext _context;
    protected IMongoDatabase _database;
    protected IMongoCollection<BsonDocument> _gates;
    protected readonly List<BsonDocument> _admissions = [];
    protected long _epoch = 13;
    ServiceProvider _services;

    void Establish()
    {
        _context = GenerationContexts.Create(WellKnownSinkTypes.MongoDB, 2, 7, _epoch);
        _database = Substitute.For<IMongoDatabase>();
        var client = Substitute.For<IMongoClient>();
        _database.Client.Returns(client);
        _database.DatabaseNamespace.Returns(new DatabaseNamespace("models"));
        _database.RunCommandAsync(Arg.Any<Command<BsonDocument>>(), Arg.Any<ReadPreference>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new BsonDocument("setName", "rs0")));
        client.StartSessionAsync(Arg.Any<ClientSessionOptions>(), Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(Substitute.For<IClientSessionHandle>()));
        _gates = Substitute.For<IMongoCollection<BsonDocument>>();
        _database.GetCollection<BsonDocument>(FenceCollections.Gates, Arg.Any<MongoCollectionSettings>()).Returns(_gates);
        _database.GetCollection<BsonDocument>(FenceCollections.Receipts, Arg.Any<MongoCollectionSettings>()).Returns(Substitute.For<IMongoCollection<BsonDocument>>());
        _gates.UpdateOneAsync(Arg.Any<IClientSessionHandle>(), Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var filter = call.Arg<FilterDefinition<BsonDocument>>().Render(new RenderArgs<BsonDocument>(BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry));
                _admissions.Add(filter);
                return Task.FromResult<UpdateResult>(new UpdateResult.Acknowledged(filter["epoch"].AsInt64 == _epoch ? 1 : 0, 0, null));
            });
        var database = Substitute.For<IDatabase>();
        database.GetReadModelDatabase("store", "namespace").Returns(_database);
        _services = new ServiceCollection().AddLogging().AddSingleton(database)
            .AddSingleton(Substitute.For<IExpandoObjectConverter>()).AddSingleton(Substitute.For<IReadModelChangeStreams>())
            .AddSingleton<IOptions<ChronicleOptions>>(Options.Create(new ChronicleOptions())).BuildServiceProvider();
        _factory = new(_services, Substitute.For<ITypeFormats>());
    }

    void Destroy() => _services.Dispose();

    protected static ISinkWrites Writes(ISink sink) => (ISinkWrites)typeof(Sink).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Single(field => typeof(ISinkWrites).IsAssignableFrom(field.FieldType)).GetValue(sink)!;
}
