// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Identifies whether a resolved MongoDB destination supports fenced transactions.
/// </summary>
public static class MongoTopology
{
    /// <summary>
    /// Refuses a standalone server, which cannot commit fenced sink transactions.
    /// </summary>
    /// <param name="database">The resolved read-model database.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An awaitable task.</returns>
    /// <exception cref="InvalidOperationException">The server is standalone.</exception>
    public static async Task RequireTransactions(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var hello = await database.RunCommandAsync<BsonDocument>(new BsonDocument("hello", 1), cancellationToken: cancellationToken);
        if (!SupportsTransactions(hello))
        {
            throw new InvalidOperationException("Projection generation fencing requires a MongoDB replica set or mongos. Start MongoDB with --replSet and initialize the replica set; a standalone server is not supported.");
        }
    }

    static bool SupportsTransactions(BsonDocument hello) =>
        (hello.TryGetValue("setName", out var setName) && setName.IsString && !string.IsNullOrWhiteSpace(setName.AsString)) ||
        hello.GetValue("msg", BsonNull.Value) == "isdbgrid";
}
