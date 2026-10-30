// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Drops all Chronicle-owned databases when the kernel is reset between integration test specs.
/// The container stays running; only the data is wiped. The kernel's bootstrap handler then
/// re-creates the system event store, namespaces, and default identity data.
/// </summary>
/// <param name="chronicleOptions"><see cref="ChronicleOptions"/> describing the active storage backend.</param>
/// <param name="mongoDBOptions"><see cref="MongoDBOptions"/> with the connection details.</param>
/// <param name="clientManager"><see cref="IMongoDBClientManager"/> used to obtain a client.</param>
/// <param name="storageOptions">Chronicle-specific database naming options.</param>
public class MongoDBKernelStateResetHandler(
    IOptions<ChronicleOptions> chronicleOptions,
    IOptions<MongoDBOptions> mongoDBOptions,
    IMongoDBClientManager clientManager,
    IOptions<MongoDBStorageOptions>? storageOptions = null) : ICanPerformKernelStateReset
{
    static readonly FrozenSet<string> _preservedDatabases = new[]
    {
        // MongoDB system databases
        "admin",
        "config",
        "local",

        // Chronicle cluster database — holds users, applications, data protection keys,
        // applied patches, and system information. Wiping these would invalidate the
        // client's cached JWT (signing keys rotate) and force every test class boundary
        // to re-bootstrap auth from scratch. The bootstrap handler can recreate them,
        // but in-flight client tokens issued before the reset would still be rejected.
        WellKnownDatabaseNames.Chronicle,
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public bool CanReset()
    {
        var type = chronicleOptions.Value.Storage.Type;
        return string.IsNullOrEmpty(type)
            || (!string.Equals(type, StorageType.Sqlite, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(type, StorageType.MsSql, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(type, StorageType.PostgreSql, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc/>
    public async Task Reset()
    {
        var url = new MongoUrl(mongoDBOptions.Value.Server);
        var settings = MongoClientSettings.FromUrl(url);
        if (mongoDBOptions.Value.DirectConnection == true)
        {
            settings.DirectConnection = true;
        }
        var client = clientManager.GetClientFor(settings);

        using var cursor = await client.ListDatabaseNamesAsync();
        var prefix = storageOptions?.Value.DatabaseNamePrefix ?? chronicleOptions.Value.Storage.DatabaseNamePrefix;
        var clusterDatabase = DatabaseNames.WithPrefix(WellKnownDatabaseNames.Chronicle, prefix);
        var droppedNames = (await cursor.ToListAsync()).Where(n =>
            !_preservedDatabases.Contains(n)
            && !string.Equals(n, clusterDatabase, StringComparison.OrdinalIgnoreCase)
            && (string.IsNullOrEmpty(prefix) || n.StartsWith(prefix, StringComparison.Ordinal))).ToList();
        foreach (var name in droppedNames)
        {
            var receipts = client.GetDatabase(name).GetCollection<BsonDocument>("generation-initialization");
            if (await receipts.CountDocumentsAsync(Builders<BsonDocument>.Filter.Exists("receipt")) != 0)
            {
                throw new InvalidOperationException("Kernel state reset is refused for an initialized generation store.");
            }
        }

        var fullyDropped = new List<string>();
        foreach (var name in droppedNames)
        {
            var database = client.GetDatabase(name);
            using var collections = await database.ListCollectionNamesAsync();
            var names = await collections.ToListAsync();
            if (names.Contains("generation-floor", StringComparer.Ordinal))
            {
                // Never drop and recreate a floor: even the reset window must retain admission.
                foreach (var collection in names.Where(_ => _ != "generation-floor" && !_.StartsWith("system.", StringComparison.Ordinal)))
                {
                    await DropWithRetry(() => database.DropCollectionAsync(collection));
                }
            }
            else
            {
                await DropDatabaseWithRetry(client, name);
                fullyDropped.Add(name);
            }
        }

        // DropDatabaseAsync acknowledges the command before MongoDB finishes the physical removal.
        // During this window any write to the dropped database name returns error 215:
        // "database is in the process of being dropped". Poll with a canary write against each
        // dropped name until the window closes so the next test class can safely write to MongoDB.
        foreach (var name in fullyDropped)
        {
            await WaitForDatabaseReady(client, name);
        }
    }

    /// <summary>
    /// MongoDB's dropDatabase is acknowledged before the physical removal completes. A subsequent
    /// drop of the same name (re-created between resets) can hit "database is currently being
    /// dropped" (error code 10055). Retry until the deadline to ride out that window.
    /// </summary>
    /// <param name="client">The <see cref="IMongoClient"/> to use.</param>
    /// <param name="name">Name of the database to drop.</param>
    static Task DropDatabaseWithRetry(IMongoClient client, string name) => DropWithRetry(() => client.DropDatabaseAsync(name));

    static async Task DropWithRetry(Func<Task> drop)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (true)
        {
            try
            {
                await drop();
                return;
            }
            catch (MongoCommandException ex) when (ex.Code == 10055 || ex.Message.Contains("currently being dropped"))
            {
                if (DateTime.UtcNow >= deadline)
                {
                    throw;
                }

                await Task.Delay(100);
            }
        }
    }

    /// <summary>
    /// Waits until MongoDB accepts writes to the given database name without error 215
    /// ("database is in the process of being dropped"). Probes by inserting a document
    /// into a sentinel collection and leaves the database in place so the next reset
    /// picks it up normally — re-dropping here would only restart the drop window.
    /// </summary>
    /// <param name="client">The <see cref="IMongoClient"/> to use.</param>
    /// <param name="name">Name of the database that was just dropped.</param>
    static async Task WaitForDatabaseReady(IMongoClient client, string name)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                await client.GetDatabase(name).CreateCollectionAsync("_ready_probe");
                return;
            }
            catch (MongoException ex) when (ex.Message.Contains("in the process of being dropped"))
            {
                await Task.Delay(100);
            }
        }
    }
}
