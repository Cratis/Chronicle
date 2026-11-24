// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB;
using Cratis.Chronicle.Storage.MongoDB.Namespaces;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

using Incarnation = Cratis.Chronicle.Concepts.NamespaceIncarnationId;

namespace Cratis.Chronicle.MongoDB.Integration.Namespaces.given;

/// <summary>Exercises real replica-set catalog/session behavior with the production marker, not a substitute fence.</summary>
/// <param name="fixture">The existing replica-set fixture; these specifications must only be run in the integration lane.</param>
public abstract class a_namespace_catalog_race(ChronicleInProcessFixture fixture) : MongoDBSpecification(fixture)
{
    protected static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    readonly List<Task> _operations = [];
    protected readonly Incarnation _old = Incarnation.New();
    protected readonly Incarnation _replacement = Incarnation.New();
    protected readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource _closeSubmitted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource _dropSubmitted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly string _databaseName = $"namespace_race_{Guid.NewGuid():N}";
    protected MongoClient _writerClient = null!;
    protected MongoClient _adminClient = null!;
    protected IMongoDatabase _database = null!;
    protected NamespaceMarkerStorage _marker = null!;
    protected NamespaceMarkerStorage _adminMarker = null!;
    protected long _eventsAfterRecreation;
    protected long _receiptsAfterRecreation;
    protected Guid _installedIdentity;

    async Task Establish()
    {
        var settings = MongoClientSettings.FromConnectionString($"mongodb://localhost:{MongoDBPort}/?directConnection=true");
        settings.SocketTimeout = Timeout;
        settings.ServerSelectionTimeout = Timeout;
        _writerClient = new(settings);
        var admin = settings.Clone();
        admin.ClusterConfigurator = builder => builder.Subscribe<CommandStartedEvent>(ObserveAdministration);
        _adminClient = new(admin);
        _database = _writerClient.GetDatabase(_databaseName);
        await _database.CreateCollectionAsync("event-witnesses");
        await _database.CreateCollectionAsync("receipt-witnesses");
        _marker = new(_database.GetCollection<BsonDocument>(WellKnownCollectionNames.NamespaceMarker));
        _adminMarker = new(_adminClient.GetDatabase(_databaseName).GetCollection<BsonDocument>(WellKnownCollectionNames.NamespaceMarker));
        (await _marker.TryOpen(_old, null)).ShouldBeTrue();
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        DisposeClients();
        base.Dispose();
    }

    protected static void Begin(IClientSessionHandle session) => session.StartTransaction(new TransactionOptions(
        readConcern: ReadConcern.Snapshot,
        readPreference: ReadPreference.Primary,
        writeConcern: WriteConcern.WMajority,
        maxCommitTime: Timeout));

    protected Task Start(Func<Task> operation)
    {
        var task = operation();
        _operations.Add(task);
        return task;
    }

    protected async Task WaitFor(Task phase, Task operation)
    {
        var completed = await Task.WhenAny(phase, operation).WaitAsync(Timeout);
        if (completed == phase) return;
        await operation;
        throw new InvalidOperationException("The operation completed before the expected race barrier.");
    }

    protected async Task CloseDropAndRecreate()
    {
        (await _adminMarker.TryClose(_old)).ShouldBeTrue();
        await DropAndRecreate();
    }

    protected async Task DropAndRecreate()
    {
        await _adminClient.DropDatabaseAsync(_databaseName);
        (await _adminMarker.TryOpen(_replacement, null)).ShouldBeTrue();
        await _adminClient.GetDatabase(_databaseName).CreateCollectionAsync("event-witnesses");
        await _adminClient.GetDatabase(_databaseName).CreateCollectionAsync("receipt-witnesses");
    }

    protected async Task WriteWitnesses(IClientSessionHandle session)
    {
        var witness = new BsonDocument { { "_id", "old-lifetime" }, { "incarnation", _old.Value.ToString("N") } };
        await _database.GetCollection<BsonDocument>("event-witnesses").InsertOneAsync(session, witness);
        await _database.GetCollection<BsonDocument>("receipt-witnesses").InsertOneAsync(session, witness);
    }

    protected async Task InspectReplacement()
    {
        _installedIdentity = (await _marker.Get())!.Incarnation.Value;
        _eventsAfterRecreation = await _database.GetCollection<BsonDocument>("event-witnesses").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        _receiptsAfterRecreation = await _database.GetCollection<BsonDocument>("receipt-witnesses").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
    }

    void ObserveAdministration(CommandStartedEvent command)
    {
        if (command.DatabaseNamespace.DatabaseName != _databaseName) return;
        if (command.CommandName == "dropDatabase") _dropSubmitted.TrySetResult();
        if (command.CommandName != "update") return;
        if (command.Command.GetValue("update", string.Empty).AsString == WellKnownCollectionNames.NamespaceMarker) _closeSubmitted.TrySetResult();
    }

    void DisposeClients()
    {
        _writerClient?.Dispose();
        _adminClient?.Dispose();
        _writerClient = null!;
        _adminClient = null!;
    }

    async Task Destroy()
    {
        _release.TrySetResult();
        if (_adminClient is null) return;
        try
        {
            await Task.WhenAll(_operations).WaitAsync(TimeSpan.FromSeconds(60));
        }
        finally
        {
            try
            {
                await _adminClient.DropDatabaseAsync(_databaseName);
            }
            finally
            {
                DisposeClients();
            }
        }
    }
}
