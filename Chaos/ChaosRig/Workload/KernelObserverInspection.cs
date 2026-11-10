// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Chaos;

internal sealed class KernelObserverInspection(string connection)
{
    public async Task<(int Documents, int InFlight)> Read(IReadOnlyList<string> expected, CancellationToken cancellation)
    {
        var client = new MongoClient(connection);
        var databases = await client.ListDatabaseNamesAsync(cancellationToken: cancellation);
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
