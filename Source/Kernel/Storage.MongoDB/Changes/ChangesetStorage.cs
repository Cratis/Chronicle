// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.Changes;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Changes;

/// <summary>
/// Represents a <see cref="IChangesetStorage"/> for storing changesets in MongoDB.
/// </summary>
/// <param name="eventStoreDatabase"><see cref="IEventStoreNamespaceDatabase"/> for the changesets.</param>
public class ChangesetStorage(
    IEventStoreNamespaceDatabase eventStoreDatabase) : IChangesetStorage
{
    /// <inheritdoc/>
    public Task BeginReplay(ReadModelName readModel) =>
        GetCollection(readModel).DeleteManyAsync(FilterDefinition<ReadModelChangeset>.Empty);

    /// <inheritdoc/>
    public Task EndReplay(ReadModelName readModel) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task Save(
        ReadModelName readModel,
        Key readModelKey,
        EventType eventType,
        EventSequenceNumber sequenceNumber,
        CorrelationId correlationId,
        IChangeset<AppendedEvent, ExpandoObject> changeset)
    {
        var collection = GetCollection(readModel);
        var key = new ReadModelChangeKey(readModelKey.ToString(), sequenceNumber, correlationId);
        var modelChangeset = new ReadModelChangeset(key, eventType, changeset.Changes);
        await collection.InsertOneAsync(modelChangeset);
    }

    IMongoCollection<ReadModelChangeset> GetCollection(ReadModelName readModel) =>
        eventStoreDatabase.GetCollection<ReadModelChangeset>($"{readModel}-changes");
}
