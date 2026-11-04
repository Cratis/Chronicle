// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.MongoDB.Integration.Observation.for_ObserverPayloadRoundTrip;

internal static class ReleasedObserverProfiler
{
    internal static async Task Enable(IMongoDatabase database)
    {
        // Retain startup updates without unrelated traffic rolling the witness out of the capped collection.
        await database.RunCommandAsync<BsonDocument>(new BsonDocument
        {
            { "profile", 1 },
            { "filter", new BsonDocument
                {
                    { "op", "update" },
                    { "ns", $"{database.DatabaseNamespace.DatabaseName}.{WellKnownCollectionNames.Observers}" }
                }
            }
        });
    }
}
