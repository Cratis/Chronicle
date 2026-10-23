// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Identifies whether a resolved MongoDB destination supports fenced transactions.
/// </summary>
public static class MongoTopology
{
    static readonly ConditionalWeakTable<IMongoClient, ConcurrentDictionary<string, Lazy<Task>>> _verified = new();

    /// <summary>
    /// Refuses a standalone server, which cannot commit fenced sink transactions.
    /// </summary>
    /// <param name="database">The resolved read-model database.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An awaitable task.</returns>
    /// <exception cref="InvalidOperationException">The server is standalone.</exception>
    public static async Task RequireTransactions(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var cache = _verified.GetValue(database.Client, static _ => new());
        var name = database.DatabaseNamespace.DatabaseName;
        var verification = cache.GetOrAdd(name, static (_, destination) => new(() => Probe(destination)), database);
        try
        {
            await verification.Value.WaitAsync(cancellationToken);
        }
        catch when (verification.Value.IsFaulted)
        {
            cache.TryRemove(KeyValuePair.Create(name, verification));
            throw;
        }
    }

    static async Task Probe(IMongoDatabase database)
    {
        var hello = await database.RunCommandAsync<BsonDocument>(new BsonDocument("hello", 1));
        if (!SupportsTransactions(hello))
        {
            throw new InvalidOperationException("Projection generation fencing requires a MongoDB replica set or mongos. Start MongoDB with --replSet and initialize the replica set; a standalone server is not supported.");
        }
    }

    static bool SupportsTransactions(BsonDocument hello) =>
        (hello.TryGetValue("setName", out var setName) && setName.IsString && !string.IsNullOrWhiteSpace(setName.AsString)) ||
        hello.GetValue("msg", BsonNull.Value) == "isdbgrid";
}
