// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clustering;
using Cratis.Chronicle.Storage.SingleWriter;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Atomically leases the system database using MongoDB server time.
/// </summary>
/// <param name="database">The system database.</param>
public class MongoDBSingleWriterLeaseStorage(IDatabase database) : ISingleWriterLeaseStorage
{
    IMongoCollection<SingleWriterLeaseDocument> Collection => database.GetCollection<SingleWriterLeaseDocument>(WellKnownCollectionNames.SingleWriterLeases)
        .WithWriteConcern(WriteConcern.WMajority).WithReadConcern(ReadConcern.Majority);

    /// <inheritdoc/>
    public async Task<SingleWriterLeaseAttempt> TryAcquire(SingleWriterLeaseClaim claim, CancellationToken cancellationToken)
    {
        var sameOwner = new BsonDocument("$eq", new BsonArray { "$owner", Literal(claim.Owner.Value) });
        var canTake = new BsonDocument("$or", new BsonArray
        {
            new BsonDocument("$eq", new BsonArray { new BsonDocument("$ifNull", new BsonArray { "$owner", BsonNull.Value }), BsonNull.Value }),
            sameOwner,
            new BsonDocument("$lte", new BsonArray { "$expiresAt", "$$NOW" })
        });
        var filter = new BsonDocument("_id", "kernel");
        var fields = Renewal(claim.Ttl);
        fields.Add("owner", Literal(claim.Owner.Value))
            .Add("clusterId", Literal(claim.ClusterId))
            .Add("serviceId", Literal(claim.ServiceId))
            .Add("hostName", Literal(claim.HostName))
            .Add("version", Literal(claim.Version))
            .Add("acquiredAt", new BsonDocument("$cond", new BsonArray { sameOwner, "$acquiredAt", "$$NOW" }));
        var conditionalFields = new BsonDocument();
        foreach (var field in fields)
        {
            conditionalFields.Add(field.Name, new BsonDocument("$cond", new BsonArray { canTake, field.Value, $"${field.Name}" }));
        }

        try
        {
            var document = await Collection.FindOneAndUpdateAsync(
                filter,
                Pipeline(conditionalFields),
                new FindOneAndUpdateOptions<SingleWriterLeaseDocument> { IsUpsert = true, ReturnDocument = ReturnDocument.After },
                cancellationToken);
            return new(document.Owner == claim.Owner.Value, Holder(document));
        }
        catch (MongoCommandException exception) when (exception.Code == 11000)
        {
            return new(false, Holder(await Collection.Find(document => document.Id == "kernel").SingleOrDefaultAsync(cancellationToken)));
        }
    }

    /// <inheritdoc/>
    public async Task<bool> Renew(SingleWriterLeaseOwner owner, TimeSpan ttl, CancellationToken cancellationToken)
    {
        var filter = new BsonDocument
        {
            { "_id", "kernel" },
            { "owner", owner.Value },
            { "$expr", new BsonDocument("$gt", new BsonArray { "$expiresAt", "$$NOW" }) }
        };
        var result = await Collection.UpdateOneAsync(filter, Pipeline(Renewal(ttl)), cancellationToken: cancellationToken);
        return result.MatchedCount == 1;
    }

    /// <inheritdoc/>
    public async Task Release(SingleWriterLeaseOwner owner, CancellationToken cancellationToken)
    {
        var filter = new BsonDocument { { "_id", "kernel" }, { "owner", owner.Value } };
        await Collection.UpdateOneAsync(filter, Pipeline(new BsonDocument("expiresAt", "$$NOW")), cancellationToken: cancellationToken);
    }

    static BsonDocument Literal(string value) => new("$literal", value);

    static BsonDocument Renewal(TimeSpan ttl)
    {
        var expiry = new BsonDocument
        {
            { "startDate", "$$NOW" },
            { "unit", "millisecond" },
            { "amount", checked((long)ttl.TotalMilliseconds) }
        };
        return new BsonDocument
        {
            { "renewedAt", "$$NOW" },
            { "expiresAt", new BsonDocument("$dateAdd", expiry) }
        };
    }

    static PipelineUpdateDefinition<SingleWriterLeaseDocument> Pipeline(BsonDocument fields) =>
        new(new[] { new BsonDocument("$set", fields) });

    static SingleWriterLeaseHolder? Holder(SingleWriterLeaseDocument? document) => document is null ? null :
        new(new(document.Owner), document.HostName, document.Version, new DateTimeOffset(document.AcquiredAt), new DateTimeOffset(document.ExpiresAt));
}
