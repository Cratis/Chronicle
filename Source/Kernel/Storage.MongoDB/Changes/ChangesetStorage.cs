// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Json;
using Cratis.Chronicle.Storage.Changes;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Changes;

/// <summary>
/// Represents a <see cref="IChangesetStorage"/> for storing changesets in MongoDB.
/// </summary>
public class ChangesetStorage(
    IEventStoreNamespaceDatabase eventStoreDatabase,
    IJsonProjectionChangesetSerializer jsonSerializer) : IChangesetStorage
{
    IMongoCollection<BsonDocument> Collection => eventStoreDatabase.GetCollection<BsonDocument>(WellKnownCollectionNames.ProjectionChangesets);

    /// <inheritdoc/>
    public async Task Save(
        ProjectionId projectionIdentifier,
        Key projectionObjectKey,
        ProjectionPath projectionPath,
        EventSequenceNumber sequenceNumber,
        CorrelationId correlationId,
        IChangeset<AppendedEvent, ExpandoObject> changeset)
    {
        // TODO: Implement
        return;
        var json = jsonSerializer.Serialize(changeset);
        var document = BsonDocument.Parse(json.ToJsonString());

        await Collection.InsertOneAsync(document);
    }
}
