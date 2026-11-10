// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Inspects persisted observer drain state in the dedicated kernel database.
/// </summary>
/// <param name="connection">The kernel MongoDB connection.</param>
internal sealed class KernelObserverInspection(string connection)
{
    /// <summary>
    /// Reads observer document and in-flight partition counts.
    /// </summary>
    /// <param name="expected">The expected observer identifiers.</param>
    /// <param name="cancellation">The inspection cancellation token.</param>
    /// <returns>The persisted document and in-flight counts.</returns>
    /// <exception cref="InvalidDataException">An observer document lacks the expected partition state.</exception>
    public async Task<(int Documents, int InFlight)> Read(IReadOnlyList<string> expected, CancellationToken cancellation)
    {
        using var client = new MongoClient(connection);
        using var databases = await client.ListDatabaseNamesAsync(cancellationToken: cancellation);
        var filter = Builders<BsonDocument>.Filter.In("_id", expected);
        var documents = new List<BsonDocument>();

        // Discover rather than duplicate Chronicle's physical database naming rules. This is a fresh, dedicated Mongo container.
        foreach (var name in await databases.ToListAsync(cancellation))
        {
            var collection = client.GetDatabase(name).GetCollection<BsonDocument>("observers");
            documents.AddRange(await collection.Find(filter).ToListAsync(cancellation));
        }
        var count = 0;
        foreach (var document in documents)
        {
            if (!document.TryGetValue("inFlightPartitions", out var partitions) || !partitions.IsBsonArray)
            {
                throw new InvalidDataException("Kernel observer document lacks an inFlightPartitions array; refusing to infer a drained state.");
            }
            count += partitions.AsBsonArray.Count;
        }
        return (documents.Count, count);
    }
}
