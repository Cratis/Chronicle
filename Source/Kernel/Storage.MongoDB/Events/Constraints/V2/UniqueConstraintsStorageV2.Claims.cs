// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints.V2;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2;

public sealed partial class UniqueConstraintsStorageV2
{
    static async Task<UniqueConstraintWriteFailure?> SaveClaim(IClientSessionHandle session, IMongoCollection<BsonDocument> collection, (BsonDocument Document, UniqueConstraintMode Mode) claim, CancellationToken cancellationToken)
    {
        var document = claim.Document;
        var value = new BsonDocument { { "Scope", document["Scope"] }, { "Value", document["Value"] } };
        var existing = await collection.Find(session, value, new FindOptions { Collation = V2Schema.Simple }).SingleOrDefaultAsync(cancellationToken);
        // This read is authoritative under the shared authority fence. A concurrent external drop could
        // let insert auto-create storage without its index; the index is never the only duplicate defense.
        if (existing is not null)
        {
            if (existing["EventSourceId"] != document["EventSourceId"]) return UniqueConstraintWriteFailure.ConflictingClaim;
            UpdateDefinition<BsonDocument> subject = document.TryGetValue("Subject", out var metadata)
                ? new BsonDocument("$set", new BsonDocument("Subject", metadata))
                : new BsonDocument("$unset", new BsonDocument("Subject", 1));
            var owned = new BsonDocument { { "_id", existing["_id"] }, { "EventSourceId", document["EventSourceId"] }, { "Scope", document["Scope"] }, { "Value", document["Value"] } };
            await collection.UpdateOneAsync(session, owned, subject, new UpdateOptions { Collation = V2Schema.Simple }, cancellationToken);
            return null;
        }

        if (claim.Mode == UniqueConstraintMode.PerEventSource)
        {
            var owner = new BsonDocument { { "Scope", document["Scope"] }, { "EventSourceId", document["EventSourceId"] } };
            await collection.DeleteManyAsync(session, owner, new DeleteOptions { Collation = V2Schema.Simple }, cancellationToken);
        }

        await collection.InsertOneAsync(session, document, cancellationToken: cancellationToken);
        return null;
    }

    static async Task<UniqueConstraintWriteFailure?> DeleteClaims(IClientSessionHandle session, IMongoCollection<BsonDocument> collection, BsonDocument filter, CancellationToken cancellationToken)
    {
        await collection.DeleteManyAsync(session, filter, new DeleteOptions { Collation = V2Schema.Simple }, cancellationToken);
        return null;
    }
}
