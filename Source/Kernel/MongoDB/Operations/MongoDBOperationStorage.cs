// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Kernel.Grains.Operations;
using Aksio.Cratis.Kernel.MongoDB.Observation;
using Aksio.Cratis.Kernel.Operations;
using Aksio.Cratis.Kernel.Persistence.Operations;
using Aksio.DependencyInversion;
using Aksio.Strings;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Operations;

/// <summary>
/// Represents an implementation of <see cref="IOperationStorage"/> for MongoDB.
/// </summary>
public class MongoDBOperationStorage : IOperationStorage
{
    readonly ProviderFor<IEventStoreDatabase> _databaseProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBOperationStorage"/> class.
    /// </summary>
    /// <param name="databaseProvider">Provider for <see cref="IEventStoreDatabase"/>.</param>
    public MongoDBOperationStorage(ProviderFor<IEventStoreDatabase> databaseProvider)
    {
        _databaseProvider = databaseProvider;
    }

    IMongoCollection<BsonDocument> Collection => _databaseProvider().GetCollection<BsonDocument>(WellKnownCollectionNames.Operations);

    /// <inheritdoc/>
    public async Task<OperationState?> Get(OperationId operationId)
    {
        var filter = GetIdFilter(operationId);
        var cursor = await Collection.FindAsync(filter).ConfigureAwait(false);
        var state = cursor.SingleOrDefault();
        if (state is null) return null;
        return DeserializeState(state);
    }

    /// <inheritdoc/>
    public Task Remove(OperationId operationId)
    {
        var filter = GetIdFilter(operationId);
        return Collection.DeleteOneAsync(filter);
    }

    /// <inheritdoc/>
    public Task Save(OperationId operationId, OperationState operationState)
    {
        var filter = GetIdFilter(operationId);
        var document = operationState.ToBsonDocument();
        document.Remove("_id");
        document[nameof(OperationState.Request).ToCamelCase()] = operationState.Request.ToBsonDocument();
        return Collection.ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = true });
    }

    /// <inheritdoc/>
    public async Task<IImmutableList<OperationState>> GetOperations()
    {
        var cursor = await Collection.FindAsync(_ => true).ConfigureAwait(false);
        var deserialized = cursor.ToList().Select(DeserializeState);
        return deserialized.ToImmutableList();
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<OperationState>> ObserveOperations()
    {
        var operations = GetOperations().GetAwaiter().GetResult();
        return Collection.Observe(operations, HandleChangesForOperations);
    }

    OperationState DeserializeState(BsonDocument document)
    {
        var requestProperty = nameof(OperationState.Request).ToCamelCase();
        var requestAsDocument = document.GetValue(requestProperty).AsBsonDocument;
        var requestType = Type.GetType(requestAsDocument["type"].AsString);
        var request = BsonSerializer.Deserialize(requestAsDocument, requestType);
        document.Remove(requestProperty);
        var state = BsonSerializer.Deserialize<OperationState>(document);
        state.Request = request;
        return state;
    }

    void HandleChangesForOperations(IChangeStreamCursor<ChangeStreamDocument<BsonDocument>> cursor, List<OperationState> operations)
    {
        foreach (var change in cursor.Current)
        {
            var changedOperation = change.FullDocument;
            if (change.OperationType == ChangeStreamOperationType.Delete)
            {
                var operation = operations.Find(_ => _.Id == (OperationId)change.DocumentKey["_id"].AsGuid);
                if (operation is not null)
                {
                    operations.Remove(operation);
                }
                continue;
            }

            var operationTypeString = changedOperation["type"].AsString;
            var operationType = Type.GetType(operationTypeString);
            if (operationType is not null)
            {
                var observer = operations.Find(_ => _.Id == (OperationId)changedOperation["_id"].AsGuid);

                var operationState = BsonSerializer.Deserialize<OperationState>(changedOperation);
                if (observer is not null)
                {
                    var index = operations.IndexOf(observer);
                    operations[index] = operationState;
                }
                else
                {
                    operations.Add(operationState);
                }
            }
        }
    }

    FilterDefinition<BsonDocument> GetIdFilter(Guid id) => Builders<BsonDocument>.Filter.Eq(new StringFieldDefinition<BsonDocument, Guid>("_id"), id);
}
