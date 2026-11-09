// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Schema;

/// <summary>
/// Inspects required features once at startup using the system database, never the append path.
/// </summary>
public class MongoDBCapabilityProbe : IMongoDBCapabilityProbe
{
    /// <summary>
    /// The reserved collection name used for capability probes.
    /// </summary>
    internal const string ProbeCollectionName = WellKnownCollectionNames.CapabilityProbe;
    static readonly TimeSpan _probeTimeout = TimeSpan.FromSeconds(10);
    static readonly string[] _unsupportedMessages = ["not supported yet", "not implemented", "Unrecognized pipeline stage"];

    readonly Func<CancellationToken, Task> _changeStreams;
    readonly Func<string, CancellationToken, Task> _collation;
    readonly Func<string, CancellationToken, Task> _cleanup;
    readonly ILogger? _logger;
    readonly TimeSpan _timeout;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBCapabilityProbe"/> class.
    /// </summary>
    /// <param name="database">The system database abstraction used by MongoDB storage.</param>
    /// <param name="options">The schema policy determining whether DDL is permitted.</param>
    /// <param name="logger">The logger for probe cleanup failures.</param>
    public MongoDBCapabilityProbe(IDatabase database, IOptions<ChronicleOptions> options, ILogger<MongoDBCapabilityProbe> logger)
        : this(database, options, logger, _probeTimeout)
    {
    }

    /// <summary>
    /// Initializes a new instance with a configurable operation and cleanup deadline.
    /// </summary>
    /// <param name="database">The system database abstraction used by MongoDB storage.</param>
    /// <param name="options">The schema policy determining whether DDL is permitted.</param>
    /// <param name="logger">The logger for probe cleanup failures.</param>
    /// <param name="timeout">The independent deadline for each operation and cleanup.</param>
    internal MongoDBCapabilityProbe(IDatabase database, IOptions<ChronicleOptions> options, ILogger<MongoDBCapabilityProbe> logger, TimeSpan timeout)
    {
        var mode = options.Value.Storage.SchemaMode;
        _changeStreams = token => ProbeChangeStreams(database, token);
        _collation = (name, token) => ProbeCollation(database, mode, name, token);
        _cleanup = (name, token) => mode == SchemaMode.Migrate
            ? Cleanup(database.GetCollection<BsonDocument>(name).Database, name, token)
            : Task.CompletedTask;
        _logger = logger;
        _timeout = timeout;
    }

    /// <summary>
    /// Initializes a new instance with fakeable driver operations.
    /// </summary>
    /// <param name="changeStreams">The change-stream operation.</param>
    /// <param name="collation">The collation operation.</param>
    /// <param name="timeout">An optional operation timeout.</param>
    internal MongoDBCapabilityProbe(Func<CancellationToken, Task> changeStreams, Func<CancellationToken, Task> collation, TimeSpan? timeout = null)
    {
        _changeStreams = changeStreams;
        _collation = (_, token) => collation(token);
        _cleanup = (_, _) => Task.CompletedTask;
        _timeout = timeout ?? _probeTimeout;
    }

    /// <inheritdoc/>
    public async Task<MongoDBCapabilityReport> Probe(CancellationToken cancellationToken)
    {
        var changeStreams = await Inspect(_changeStreams, cancellationToken);
        var collectionName = $"{WellKnownCollectionNames.CapabilityProbePrefix}{ObjectId.GenerateNewId()}";
        MongoDBCapability collation;
        try
        {
            collation = await Inspect(token => _collation(collectionName, token), cancellationToken);
        }
        finally
        {
            // Classification has its own deadline; slow cleanup must never turn Unsupported into Inconclusive.
            await Cleanup(collectionName);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return new(changeStreams, collation);
    }

    static async Task ProbeChangeStreams(IDatabase database, CancellationToken cancellationToken)
    {
        var mongo = database.GetCollection<BsonDocument>(ProbeCollectionName).Database;
        using var cursor = await mongo.WatchAsync(new ChangeStreamOptions { MaxAwaitTime = TimeSpan.FromMilliseconds(1) }, cancellationToken);
        await cursor.MoveNextAsync(cancellationToken);
    }

    static async Task ProbeCollation(IDatabase database, SchemaMode mode, string collectionName, CancellationToken cancellationToken)
    {
        var collection = database.GetCollection<BsonDocument>(collectionName);
        if (mode != SchemaMode.Migrate)
        {
            // Best effort without DDL rights: some compatible services accept find collation but reject index collation.
            using var cursor = await collection.FindAsync(
                FilterDefinition<BsonDocument>.Empty,
                new FindOptions<BsonDocument>
                {
                    Collation = Collation.Simple,
                    Limit = 1,
                    MaxTime = _probeTimeout
                },
                cancellationToken);
            await cursor.MoveNextAsync(cancellationToken);
            return;
        }

        await collection.Database.CreateCollectionAsync(collectionName, cancellationToken: cancellationToken);
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("value"),
                new CreateIndexOptions { Name = "capability_collation", Collation = Collation.Simple }),
            cancellationToken: cancellationToken);
    }

    static async Task Cleanup(IMongoDatabase database, string collectionName, CancellationToken cancellationToken)
    {
        // Only drop this run's collection or timestamped leftovers well outside the probe deadline.
        await database.DropCollectionAsync(collectionName, cancellationToken);
        using var names = await database.ListCollectionNamesAsync(
            new ListCollectionNamesOptions
            {
                Filter = new BsonDocument("name", new BsonRegularExpression($"^{WellKnownCollectionNames.CapabilityProbePrefix}"))
            },
            cancellationToken);
        var cutoff = DateTime.UtcNow.AddDays(-1);
        while (await names.MoveNextAsync(cancellationToken))
        {
            foreach (var name in names.Current)
            {
                if (IsStaleProbeCollection(name, cutoff))
                {
                    await database.DropCollectionAsync(name, cancellationToken);
                }
            }
        }
    }

    static bool IsStaleProbeCollection(string name, DateTime cutoff) =>
        name.StartsWith(WellKnownCollectionNames.CapabilityProbePrefix, StringComparison.Ordinal) &&
        ObjectId.TryParse(name[WellKnownCollectionNames.CapabilityProbePrefix.Length..], out var id) &&
        id.CreationTime < cutoff;

    async Task Cleanup(string collectionName)
    {
        using var cleanup = new CancellationTokenSource(_timeout);
        try
        {
            await _cleanup(collectionName, cleanup.Token).WaitAsync(cleanup.Token);
        }
        catch (Exception)
        {
            // Cleanup must not hide the original unsupported-feature result or expose connection details.
            _logger?.CapabilityProbeCleanupFailed();
        }
    }

    static MongoDBCapability Classify(Exception exception)
    {
        if (exception is MongoCommandException command)
        {
            if (command.Code is 13 or 18)
            {
                return MongoDBCapability.Inconclusive("The probe was not authorized.");
            }

            if (command.Code == 40573 || command.Message.Contains("only supported on replica sets", StringComparison.OrdinalIgnoreCase))
            {
                return MongoDBCapability.Inconclusive("Change streams require a replica set.");
            }

            if (command.Code is 115 or 238 || _unsupportedMessages.Any(message => command.Message.Contains(message, StringComparison.OrdinalIgnoreCase)))
            {
                return MongoDBCapability.Unsupported("The server explicitly rejected the feature.");
            }
        }

        // Never retain raw exception messages: they may contain connection strings or credentials.
        return MongoDBCapability.Inconclusive(exception is OperationCanceledException or TimeoutException
            ? "The probe timed out."
            : $"The probe failed ({exception.GetType().Name}).");
    }

    async Task<MongoDBCapability> Inspect(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_timeout);
        try
        {
            await operation(timeout.Token).WaitAsync(timeout.Token);
            cancellationToken.ThrowIfCancellationRequested();
            return MongoDBCapability.Supported;
        }
        catch (Exception exception)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Classify(exception);
        }
    }
}
