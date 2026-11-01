// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Events.Constraints;
using Cratis.Orleans.Storage.MongoDB.Serialization;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBIndexProvisioner;

[Collection("MongoDB schema authentication")]
public class when_runtime_identity_cannot_create_indexes : Specification
{
    readonly ConcurrentQueue<string> _commands = new();
    IContainer _container;
    IMongoClient _admin;
    IDatabase _runtime;
    IMongoDatabase _runtimeJobs;
    ILogger<UniqueConstraintsStorage> _logger;
    MongoCommandException? _newConstraintFailure;
    int _runtimeDdl;
    IMongoDatabase _namespace;
    MongoDBIndexReport _verified;
    MongoCommandException? _denied;
    string _prefix;
    long _claims;

    async Task Establish()
    {
        BsonSerializer.RegisterSerializationProvider(new ConstraintDefinitionSerializationProvider());
        SpecSerializationSetup.Initialize();
        _prefix = $"s{Guid.NewGuid().ToString("N")[..8]}_";
        var adminPassword = Guid.NewGuid().ToString("N");
        var runtimePassword = Guid.NewGuid().ToString("N");
        _container = new ContainerBuilder("mongo:8.2")
            .WithEnvironment("MONGO_INITDB_ROOT_USERNAME", "schema-spec")
            .WithEnvironment("MONGO_INITDB_ROOT_PASSWORD", adminPassword)
            .WithPortBinding(27017, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(27017))
            .Build();
        await _container.StartAsync();
        var server = $"mongodb://{_container.Hostname}:{_container.GetMappedPublicPort(27017)}";
        var adminSettings = MongoClientSettings.FromConnectionString(server);
        adminSettings.Credential = MongoCredential.CreateCredential("admin", "schema-spec", adminPassword);
        _admin = new MongoClient(adminSettings);
        var adminManager = Substitute.For<IMongoDBClientManager>();
        adminManager.GetClientFor(Arg.Any<MongoClientSettings>()).Returns(_admin);
        var mongoOptions = Options.Create(new MongoDBOptions { Server = server, Database = "chronicle" });
        var storageOptions = Options.Create(new MongoDBStorageOptions { DatabaseNamePrefix = _prefix });
        var adminDatabase = new Database(adminManager, mongoOptions, Substitute.For<ICustomSerializers>(), storageOptions);
        _namespace = adminDatabase.GetEventStoreDatabase("orders").GetNamespaceDatabase("Default").GetCollection<BsonDocument>(WellKnownCollectionNames.EventSequences).Database;
        var constraints = new ConstraintsStorage(adminDatabase.GetEventStoreDatabase("orders"));
        await constraints.SaveDefinition(new UniqueConstraintDefinition("email", []));
        await constraints.SaveDefinition(new UniqueConstraintDefinition("retained-email", []) { Mode = UniqueConstraintMode.PerValue });
        await _namespace.CreateCollectionAsync("event-log+email+constraint");
        await _namespace.CreateCollectionAsync("event-log+retained-email+values+constraint");
        await new MongoDBIndexProvisioner(adminDatabase, adminManager, mongoOptions, storageOptions, NullLoggerFactory.Instance).Migrate(["orders"]);
        await CreateRuntimeIdentity(runtimePassword);
        var runtimeSettings = MongoClientSettings.FromConnectionString(server);
        runtimeSettings.Credential = MongoCredential.CreateCredential("admin", "runtime-spec", runtimePassword);
        runtimeSettings.ClusterConfigurator = cluster => cluster.Subscribe<CommandStartedEvent>(command => _commands.Enqueue(command.CommandName));
        var runtimeClient = new MongoClient(runtimeSettings);
        var runtimeManager = Substitute.For<IMongoDBClientManager>();
        runtimeManager.GetClientFor(Arg.Any<MongoClientSettings>()).Returns(runtimeClient);
        _runtime = new Database(runtimeManager, mongoOptions, Substitute.For<ICustomSerializers>(), storageOptions);
        _runtimeJobs = runtimeClient.GetDatabase(DatabaseNames.ForEventStoreNamespace("orders", "Default", _prefix));
        var collection = _runtime.GetEventStoreDatabase("orders").GetNamespaceDatabase("Default").GetCollection<BsonDocument>(WellKnownCollectionNames.EventLog);
        try
        {
            await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(new BsonDocument("denied", 1), new CreateIndexOptions { Name = "denied" }));
        }
        catch (MongoCommandException exception) when (exception.Code == 13)
        {
            _denied = exception;
        }
        _commands.Clear();
        _verified = await new MongoDBIndexProvisioner(_runtime, runtimeManager, mongoOptions, storageOptions, NullLoggerFactory.Instance).Verify(["orders"]);
    }

    async Task Because()
    {
        await given.runtime_index_paths.Exercise(_runtime, _runtimeJobs);
        _runtimeDdl = _commands.Count(command => given.a_schema_database.IsAnyOf(command, "createIndexes", "dropIndexes"));
        _claims = await _namespace.GetCollection<BsonDocument>("event-log+email+constraint").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        _logger = Substitute.For<ILogger<UniqueConstraintsStorage>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        try
        {
            var database = _runtime.GetEventStoreDatabase("orders").GetNamespaceDatabase("Default");
            await new UniqueConstraintsStorage(database, EventSequenceId.Log, _logger).Save(EventSourceId.New(), new("new-constraint", []), 2, "three");
        }
        catch (MongoCommandException exception) when (exception.Code == 13)
        {
            _newConstraintFailure = exception;
        }
    }

    [Fact] void should_prove_index_ddl_is_denied() => _denied.ShouldNotBeNull();
    [Fact] void should_verify_with_the_restricted_identity() => _verified.IsCurrent.ShouldBeTrue();
    [Fact] void should_write_without_index_privileges() => _claims.ShouldEqual(1L);
    [Fact] void should_issue_no_runtime_index_ddl_for_provisioned_collections() => _runtimeDdl.ShouldEqual(0);
    [Fact] void should_fail_clearly_for_a_new_unscoped_constraint() => _newConstraintFailure.ShouldNotBeNull();
    [Fact] void should_log_the_constraint_provisioning_action() => _logger.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "Log" && (LogLevel)call.GetArguments()[0]! == LogLevel.Error).ShouldBeTrue();

    async Task CreateRuntimeIdentity(string password)
    {
        var admin = _admin.GetDatabase("admin");
        await admin.RunCommandAsync<BsonDocument>(new BsonDocument
        {
            { "createRole", "runtime-without-ddl" },
            {
                "privileges",
                new BsonArray
                {
                    new BsonDocument
                    {
                        { "resource", new BsonDocument { { "db", "" }, { "collection", "" } } },
                        { "actions", new BsonArray { "find", "insert", "update", "remove", "listCollections", "listIndexes" } }
                    }
                }
            },
            { "roles", new BsonArray() }
        });
        await admin.RunCommandAsync<BsonDocument>(new BsonDocument
        {
            { "createUser", "runtime-spec" },
            { "pwd", password },
            { "roles", new BsonArray { new BsonDocument { { "role", "runtime-without-ddl" }, { "db", "admin" } } } }
        });
    }

    async Task Destroy()
    {
        if (_container is not null) await _container.DisposeAsync();
    }
}
