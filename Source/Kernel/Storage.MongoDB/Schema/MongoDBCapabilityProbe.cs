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
    internal const string ProbeCollectionName = "chronicle_capability_probe";
    static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(10);
    static readonly string[] UnsupportedMessages = ["not supported yet", "not implemented", "Unrecognized pipeline stage"];

    readonly Func<CancellationToken, Task> _changeStreams;
    readonly Func<CancellationToken, Task> _collation;
    readonly TimeSpan _timeout;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBCapabilityProbe"/> class.
    /// </summary>
    /// <param name="database">The system database abstraction used by MongoDB storage.</param>
    /// <param name="options">The schema policy determining whether DDL is permitted.</param>
    /// <param name="logger">The logger for probe cleanup failures.</param>
    public MongoDBCapabilityProbe(IDatabase database, IOptions<ChronicleOptions> options, ILogger<MongoDBCapabilityProbe> logger)
        : this(token => ProbeChangeStreams(database, token), token => ProbeCollation(database, options.Value.Storage.SchemaMode, logger, token))
    {
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
        _collation = collation;
        _timeout = timeout ?? ProbeTimeout;
    }

    /// <inheritdoc/>
    public async Task<MongoDBCapabilityReport> Probe(CancellationToken cancellationToken)
    {
        var changeStreams = await Inspect(_changeStreams, cancellationToken);
        var collation = await Inspect(_collation, cancellationToken);
        return new(changeStreams, collation);
    }

    static async Task ProbeChangeStreams(IDatabase database, CancellationToken cancellationToken)
    {
        var mongo = database.GetCollection<BsonDocument>(ProbeCollectionName).Database;
        using var cursor = await mongo.WatchAsync(new ChangeStreamOptions { MaxAwaitTime = TimeSpan.FromMilliseconds(1) }, cancellationToken);
        await cursor.MoveNextAsync(cancellationToken);
    }

    static async Task ProbeCollation(IDatabase database, SchemaMode mode, ILogger logger, CancellationToken cancellationToken)
    {
        var collection = database.GetCollection<BsonDocument>(ProbeCollectionName);
        if (mode != SchemaMode.Migrate)
        {
            // Best effort without DDL rights: some compatible services accept find collation but reject index collation.
            using var cursor = await collection.FindAsync(FilterDefinition<BsonDocument>.Empty, new FindOptions<BsonDocument>
            {
                Collation = Collation.Simple,
                Limit = 1,
                MaxTime = ProbeTimeout
            }, cancellationToken);
            await cursor.MoveNextAsync(cancellationToken);
            return;
        }

        // Do not touch a pre-existing collection at the reserved name, even if createCollection is idempotent.
        using var names = await collection.Database.ListCollectionNamesAsync(new ListCollectionNamesOptions
        {
            Filter = new BsonDocument("name", ProbeCollectionName)
        }, cancellationToken);
        if (await names.AnyAsync(cancellationToken))
        {
            throw new InvalidOperationException("The reserved capability probe collection already exists.");
        }

        await collection.Database.CreateCollectionAsync(ProbeCollectionName, cancellationToken: cancellationToken);
        try
        {
            await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("value"),
                new CreateIndexOptions { Name = "capability_collation", Collation = Collation.Simple }), cancellationToken: cancellationToken);
        }
        finally
        {
            // Dropping the throwaway collection drops its index too, even after an unsupported response or cancellation.
            await Cleanup(collection.Database, logger);
        }
    }

    static async Task Cleanup(IMongoDatabase database, ILogger logger)
    {
        using var cleanup = new CancellationTokenSource(ProbeTimeout);
        try
        {
            await database.DropCollectionAsync(ProbeCollectionName, cleanup.Token);
        }
        catch (Exception)
        {
            // Cleanup must not hide the original unsupported-feature result or expose connection details.
            logger.CapabilityProbeCleanupFailed();
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

            if (command.Code is 115 or 238 || UnsupportedMessages.Any(message => command.Message.Contains(message, StringComparison.OrdinalIgnoreCase)))
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
