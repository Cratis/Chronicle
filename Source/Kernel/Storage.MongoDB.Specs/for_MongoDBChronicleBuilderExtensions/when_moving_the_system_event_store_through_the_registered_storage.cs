// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.DependencyInjection;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.MongoDB.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBChronicleBuilderExtensions;

/// <summary>
/// The patch that moves the System event store's databases asks the <see cref="IStorage"/> the kernel registers.
/// Whatever implements that has to do the move: a default that does nothing leaves the patch recorded as applied while
/// the data stays where it was.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB server.</param>
[Collection(MongoDBCollection.Name)]
public class when_moving_the_system_event_store_through_the_registered_storage(MongoDBFixture fixture) : Specification
{
    readonly string _prefix = $"reg_{Guid.NewGuid().ToString("N")[..8]}_";
    IHost _host;
    MongoClient _client;

    async Task Establish()
    {
        _client = new MongoClient(fixture.ConnectionString);
        await Insert("System+es", "namespaces", new BsonDocument("_id", "Default"));
        await Insert("System+es", "event-types", new BsonDocument("_id", "legacy-only"));
        await Insert("System+es+Default", "event-log", new BsonDocument("_id", 0), new BsonDocument("_id", 1));
        await Insert("System", "users", new BsonDocument("_id", "a-user"));

        _host = Host.CreateDefaultBuilder()
            .AddCratisArc()
            .AddCratisMongoDB(options =>
            {
                options.Server = fixture.ConnectionString;
                options.Database = $"{_prefix}chronicle";
            })
            .UseDefaultServiceProvider(options => options.ValidateOnBuild = false)
            .UseOrleans(silo =>
            {
                silo.UseLocalhostClustering();
                silo.Services.AddTypeDiscovery();
                silo.Services.AddBindingsByConvention();
                silo.Services.AddSelfBindings();
                silo.Services.AddSingleton(Substitute.For<IJobTypes>());
                silo.Services.AddSingleton(Substitute.For<ICustomSerializers>());
                silo.AddChronicleToSilo(builder => builder.WithMongoDB(new ChronicleOptions
                {
                    Storage = new Configuration.Storage
                    {
                        ConnectionDetails = fixture.ConnectionString,
                        DatabaseNamePrefix = _prefix
                    }
                }));
            })
            .Build();
    }

    async Task Because() => await _host.Services.GetRequiredService<IStorage>().MoveSystemEventStoreTo(SystemEventStoreLocation.Configured);

    [Fact] async Task should_leave_only_the_configured_databases() => (await Databases()).ShouldEqual(["_system", "_system+es", "_system+es+Default"]);
    [Fact] async Task should_move_the_event_log() => (await Count("_system+es+Default", "event-log")).ShouldEqual(2L);
    [Fact] async Task should_move_the_event_store_state() => (await Count("_system+es", "event-types")).ShouldEqual(1L);
    [Fact] async Task should_move_the_read_models() => (await Count("_system", "users")).ShouldEqual(1L);

    async Task Destroy()
    {
        _host?.Dispose();
        foreach (var name in await (await _client.ListDatabaseNamesAsync()).ToListAsync())
        {
            if (name.StartsWith(_prefix, StringComparison.Ordinal))
            {
                await _client.DropDatabaseAsync(name);
            }
        }
    }

    Task Insert(string database, string collection, params BsonDocument[] documents) =>
        _client.GetDatabase($"{_prefix}{database}").GetCollection<BsonDocument>(collection).InsertManyAsync(documents);

    Task<long> Count(string database, string collection) =>
        _client.GetDatabase($"{_prefix}{database}").GetCollection<BsonDocument>(collection).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);

    async Task<string[]> Databases() =>
        (await (await _client.ListDatabaseNamesAsync()).ToListAsync())
            .Where(_ => _.StartsWith(_prefix, StringComparison.Ordinal))
            .Select(_ => _[_prefix.Length..])
            .Where(_ => !_.StartsWith("chronicle", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();
}
