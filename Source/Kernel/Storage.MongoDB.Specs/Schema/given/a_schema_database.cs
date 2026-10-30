// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.Orleans.Storage.MongoDB.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.given;

/// <summary>
/// Provides isolated real databases and observes commands without recording command payloads.
/// </summary>
/// <param name="fixture">The shared MongoDB fixture.</param>
public abstract class a_schema_database(MongoDBFixture fixture) : Specification
{
    protected const string Store = "orders";
    protected const string Namespace = "Default";
    protected readonly ConcurrentQueue<string> _commands = new();
    protected readonly ConcurrentQueue<(string Database, string Collection)> _queries = new();
    protected readonly ConcurrentQueue<(string Database, string Collection)> _indexTargets = new();
    protected string _prefix;
    protected IMongoClient _client;
    protected IMongoDBClientManager _clients;
    protected Database _database;
    protected IMongoDatabase _namespace;
    protected MongoDBIndexProvisioner _provisioner;
    protected IOptions<MongoDBOptions> _mongoOptions;
    protected IOptions<MongoDBStorageOptions> _storageOptions;

    protected int DdlCommands => _commands.Count(command => IsAnyOf(command, "createIndexes", "dropIndexes", "create", "drop"));

    protected string ConnectionString => fixture.ConnectionString;

    internal static bool IsAnyOf(string value, params string[] candidates) => candidates.Contains(value, StringComparer.Ordinal);

    /// <summary>
    /// Stores definitions for both runtime claim modes without overloading one name with different modes.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected async Task SaveConstraintDefinitions()
    {
        var constraints = new ConstraintsStorage(_database.GetEventStoreDatabase(Store));
        await constraints.SaveDefinition(new UniqueConstraintDefinition("email", []));
        await constraints.SaveDefinition(new UniqueConstraintDefinition("retained-email", []) { Mode = UniqueConstraintMode.PerValue });
    }

    protected async Task<IReadOnlyList<BsonDocument>> Indexes(IMongoDatabase database, string collection)
    {
        using var cursor = await database.GetCollection<BsonDocument>(collection).Indexes.ListAsync();
        return await cursor.ToListAsync();
    }

    void Establish()
    {
        BsonSerializer.RegisterSerializationProvider(new ConstraintDefinitionSerializationProvider());
        SpecSerializationSetup.Initialize();
        _prefix = $"s{Guid.NewGuid().ToString("N")[..8]}_";
        var settings = MongoClientSettings.FromConnectionString(ConnectionString);
        settings.ClusterConfigurator = cluster => cluster.Subscribe<CommandStartedEvent>(command =>
        {
            _commands.Enqueue(command.CommandName);
            if (command.CommandName == "find") _queries.Enqueue((command.DatabaseNamespace.DatabaseName, command.Command["find"].AsString));
            if (IsAnyOf(command.CommandName, "createIndexes", "dropIndexes")) _indexTargets.Enqueue((command.DatabaseNamespace.DatabaseName, command.Command[command.CommandName].AsString));
        });
        _client = new MongoClient(settings);
        _clients = Substitute.For<IMongoDBClientManager>();
        _clients.GetClientFor(Arg.Any<MongoClientSettings>()).Returns(_client);
        _mongoOptions = Options.Create(new MongoDBOptions { Server = ConnectionString, Database = "chronicle" });
        _storageOptions = Options.Create(new MongoDBStorageOptions { DatabaseNamePrefix = _prefix });
        _database = new Database(_clients, _mongoOptions, Substitute.For<ICustomSerializers>(), _storageOptions);
        _namespace = _database.GetEventStoreDatabase(Store).GetNamespaceDatabase(Namespace).GetCollection<BsonDocument>(WellKnownCollectionNames.EventSequences).Database;
        _provisioner = new(_database, _clients, _mongoOptions, _storageOptions, NullLoggerFactory.Instance);
    }

    async Task Destroy()
    {
        using var cursor = await _client.ListDatabaseNamesAsync();
        foreach (var name in (await cursor.ToListAsync()).Where(name => name.StartsWith(_prefix, StringComparison.Ordinal)))
        {
            await _client.DropDatabaseAsync(name);
        }
    }
}
