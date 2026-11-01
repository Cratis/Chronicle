// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.MongoDB.Integration.Observation.for_ObserverPayloadRoundTrip;

/// <summary>
/// Waits for the released kernel's failed-partition document, not merely its observer state.
/// </summary>
public static class ReleasedFailedPartitionWait
{
    /// <summary>
    /// Polls failed partitions belonging to the chosen observer until the chosen partition is persisted.
    /// </summary>
    /// <param name="database">The event store namespace database.</param>
    /// <param name="observerId">The isolated reactor's identifier.</param>
    /// <param name="partition">The event source that must have failed.</param>
    /// <param name="cancellationToken">Cancels the wait.</param>
    /// <returns>The persisted failed-partition document.</returns>
    /// <exception cref="TimeoutException">No matching document was persisted within the deadline.</exception>
    public static async Task<BsonDocument> WaitFor(IMongoDatabase database, string observerId, string partition, CancellationToken cancellationToken)
    {
        var collection = database.GetCollection<BsonDocument>(WellKnownCollectionNames.FailedPartitions);
        var filter = Builders<BsonDocument>.Filter.Eq("observerId", observerId);
        List<BsonDocument> observed = [];
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(45));
        try
        {
            while (true)
            {
                observed = await collection.Find(filter).ToListAsync(deadline.Token);
                var failed = observed.SingleOrDefault(document => document["partition"].AsBsonDocument.GetValue("value", BsonNull.Value) == new BsonString(partition));
                if (failed is not null) return failed;
                await Task.Delay(TimeSpan.FromMilliseconds(200), deadline.Token);
            }
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            var partitions = string.Join(", ", observed.Select(document => document.GetValue("partition", BsonNull.Value).ToString()));
            throw new TimeoutException(
                $"Released kernel did not persist {database.DatabaseNamespace.DatabaseName}.{WellKnownCollectionNames.FailedPartitions} for observer '{observerId}', partition '{partition}'. Last observed {observed.Count} failed partitions for that observer: [{partitions}].",
                exception);
        }
    }
}
