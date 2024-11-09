// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Storage.Changes;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Changes;

/// <summary>
/// Represents a <see cref="IChangesetStorage"/> for storing changesets in MongoDB.
/// </summary>
/// <param name="eventStoreDatabase"><see cref="IEventStoreNamespaceDatabase"/> for the changesets.</param>
///
public class ChangesetStorage(
    IEventStoreNamespaceDatabase eventStoreDatabase) : IChangesetStorage
{
#pragma warning disable IDE0051,RCS1213 // Remove unused private members
    IMongoCollection<BsonDocument> Collection => eventStoreDatabase.GetCollection<BsonDocument>(WellKnownCollectionNames.Changesets);
#pragma warning restore IDE0051,RCS1213 // Remove unused private members

    /// <inheritdoc/>
    public Task Save(
        ProjectionId projectionIdentifier,
        Key projectionObjectKey,
        ProjectionPath projectionPath,
        EventSequenceNumber sequenceNumber,
        CorrelationId correlationId,
        IChangeset<AppendedEvent, ExpandoObject> changeset)
    {
        return Task.CompletedTask;
    }
}
