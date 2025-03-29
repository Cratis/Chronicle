// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Models;
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
    public Task BeginReplay(ModelName model) =>
        GetCollection(model).DeleteManyAsync(FilterDefinition<ModelChangeset>.Empty);

    /// <inheritdoc/>
    public Task EndReplay(ModelName model) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task Save(
        ModelName model,
        Key modelKey,
        EventType eventType,
        EventSequenceNumber sequenceNumber,
        CorrelationId correlationId,
        IChangeset<AppendedEvent, ExpandoObject> changeset)
    {
        var collection = GetCollection(model);
        var key = new ModelChangeKey(modelKey.ToString(), sequenceNumber, correlationId);
        var modelChangeset = new ModelChangeset(key, eventType, changeset.Changes);
        await collection.InsertOneAsync(modelChangeset);
    }

    IMongoCollection<ModelChangeset> GetCollection(ModelName model) =>
        eventStoreDatabase.GetCollection<ModelChangeset>($"{model}-changes");
}
