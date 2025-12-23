// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Monads;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

#pragma warning disable CA1849, MA0042 // MongoDB breaks the Orleans task model internally, so it won't return to the task scheduler
#pragma warning disable SA1201, SA1204 // Member ordering

/// <summary>
/// Represents an implementation of <see cref="ISink"/> for working with projections in MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Sink"/> class.
/// </remarks>
/// <param name="readModel">The <see cref="ReadModelDefinition"/> the sink is for.</param>
/// <param name="converter"><see cref="IMongoDBConverter"/> for dealing with conversion.</param>
/// <param name="collections">Provider for <see cref="ISinkCollections"/> to use.</param>
/// <param name="changesetConverter">Provider for <see cref="IChangesetConverter"/> for converting changesets.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between documents and <see cref="ExpandoObject"/>.</param>
public class Sink(
    ReadModelDefinition readModel,
    IMongoDBConverter converter,
    ISinkCollections collections,
    IChangesetConverter changesetConverter,
    IExpandoObjectConverter expandoObjectConverter) : ISink
{
    const int MaxBulkOperations = 1000;

    /// <summary>
    /// Maximum size in bytes for a bulk write operation.
    /// MongoDB's limit for bulk operations is 48MB, individual documents are limited to 16MB.
    /// </summary>
    const int MaxBulkSizeInBytes = 48 * 1024 * 1024;

    readonly List<WriteModel<BsonDocument>> _bulkOperations = [];
    readonly Dictionary<int, (Key EventSourceId, EventSequenceNumber SequenceNumber)> _bulkOperationMetadata = [];
    int _currentBulkSize;
    bool _isBulkMode;

    /// <inheritdoc/>
    public SinkTypeName Name => "MongoDB";

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.MongoDB;

    /// <inheritdoc/>
    public async Task<ExpandoObject?> FindOrDefault(Key key)
    {
        var collection = Collection;

        using var result = await collection.FindAsync(Builders<BsonDocument>.Filter.Eq("_id", converter.ToBsonValue(key)));
        var instance = result.SingleOrDefault();
        if (instance != default)
        {
            return expandoObjectConverter.ToExpandoObject(instance, readModel.GetSchemaForLatestGeneration());
        }

        return default;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<FailedPartition>> ApplyChanges(
        Key key,
        IChangeset<AppendedEvent, ExpandoObject> changeset,
        EventSequenceNumber eventSequenceNumber)
    {
        var filter = changeset.HasJoined() ?
            FilterDefinition<BsonDocument>.Empty :
            Builders<BsonDocument>.Filter.Eq("_id", converter.ToBsonValue(key));

        if (changeset.HasBeenRemoved())
        {
            if (_isBulkMode)
            {
                AddToBulk(new DeleteOneModel<BsonDocument>(filter), key, eventSequenceNumber);
                return await FlushBulkIfNeeded();
            }

            await Collection.DeleteOneAsync(filter);
            return [];
        }

        // Run through and remove all children affected by ChildRemovedFromAll
        foreach (var childRemoved in changeset.Changes.OfType<ChildRemovedFromAll>())
        {
            await RemoveChildFromAll(key, childRemoved);
        }

        var converted = await changesetConverter.ToUpdateDefinition(key, changeset, eventSequenceNumber);
        if (!converted.hasChanges) return [];

        if (_isBulkMode)
        {
            var updateModel = new UpdateOneModel<BsonDocument>(filter, converted.UpdateDefinition)
            {
                IsUpsert = true,
                ArrayFilters = converted.ArrayFilters
            };
            AddToBulk(updateModel, key, eventSequenceNumber);
            return await FlushBulkIfNeeded();
        }

        await Collection.UpdateOneAsync(
            filter,
            converted.UpdateDefinition,
            new UpdateOptions
            {
                IsUpsert = true,
                ArrayFilters = converted.ArrayFilters
            });
        return [];
    }

    /// <inheritdoc/>
    public Task BeginBulk()
    {
        _isBulkMode = true;
        _bulkOperations.Clear();
        _bulkOperationMetadata.Clear();
        _currentBulkSize = 0;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task EndBulk()
    {
        if (_bulkOperations.Count > 0)
        {
            await ExecuteBulk();
        }
        _isBulkMode = false;
        _bulkOperations.Clear();
        _bulkOperationMetadata.Clear();
        _currentBulkSize = 0;
    }

    /// <inheritdoc/>
    public Task PrepareInitialRun() => collections.PrepareInitialRun();

    /// <inheritdoc/>
    public async Task BeginReplay(Chronicle.Storage.Sinks.ReplayContext context)
    {
        await collections.BeginReplay(context);
        await BeginBulk();
    }

    /// <inheritdoc/>
    public async Task ResumeReplay(Chronicle.Storage.Sinks.ReplayContext context)
    {
        await collections.ResumeReplay(context);
        await BeginBulk();
    }

    /// <inheritdoc/>
    public async Task EndReplay(Chronicle.Storage.Sinks.ReplayContext context)
    {
        await EndBulk();
        await collections.EndReplay(context);
    }

    /// <inheritdoc/>
    public async Task<Option<Key>> TryFindRootKeyByChildValue(PropertyPath childPropertyPath, object childValue)
    {
        var collection = Collection;

        var mongoPropertyPath = childPropertyPath.ToMongoDB();
        var bsonValue = childValue.ToBsonValue();

        var filter = Builders<BsonDocument>.Filter.Eq(mongoPropertyPath, bsonValue);

        using var result = await collection.FindAsync(
            filter,
            new FindOptions<BsonDocument>
            {
                Projection = Builders<BsonDocument>.Projection.Include("_id"),
                Limit = 1
            });

        var document = await result.FirstOrDefaultAsync();
        if (document is not null && document.TryGetValue("_id", out var idValue))
        {
            var key = new Key(idValue.IsGuid ? idValue.AsGuid : idValue.ToString()!, ArrayIndexers.NoIndexers);
            return new Option<Key>(key);
        }

        return Option<Key>.None();
    }

    /// <inheritdoc/>
    public async Task EnsureIndexes()
    {
        if (readModel.Indexes.Count == 0)
        {
            return;
        }

        var collection = Collection;
        var existingIndexes = await GetExistingIndexNamesAsync(collection);

        foreach (var indexDefinition in readModel.Indexes)
        {
            var indexName = $"chronicle_idx_{indexDefinition.PropertyPath.Path.Replace('.', '_')}";

            if (existingIndexes.Contains(indexName))
            {
                continue;
            }

            var indexModel = new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending(indexDefinition.PropertyPath.Path),
                new CreateIndexOptions { Name = indexName, Background = true });

            await collection.Indexes.CreateOneAsync(indexModel);
        }
    }

    async Task<HashSet<string>> GetExistingIndexNamesAsync(IMongoCollection<BsonDocument> collection)
    {
        var indexNames = new HashSet<string>();
        using var cursor = await collection.Indexes.ListAsync();
        await cursor.ForEachAsync(index =>
        {
            if (index.TryGetValue("name", out var nameValue))
            {
                indexNames.Add(nameValue.AsString);
            }
        });
        return indexNames;
    }

    void AddToBulk(WriteModel<BsonDocument> operation, Key key, EventSequenceNumber eventSequenceNumber)
    {
        var operationIndex = _bulkOperations.Count;
        _bulkOperations.Add(operation);
        _bulkOperationMetadata[operationIndex] = (key, eventSequenceNumber);

        var estimatedSize = EstimateOperationSize(operation);
        _currentBulkSize += estimatedSize;
    }

    async Task<IEnumerable<FailedPartition>> FlushBulkIfNeeded()
    {
        if (_bulkOperations.Count >= MaxBulkOperations || _currentBulkSize >= MaxBulkSizeInBytes)
        {
            return await ExecuteBulk();
        }
        return Array.Empty<FailedPartition>();
    }

    async Task<IEnumerable<FailedPartition>> ExecuteBulk()
    {
        if (_bulkOperations.Count == 0)
        {
            return [];
        }

        try
        {
            await Collection.BulkWriteAsync(_bulkOperations);
            _bulkOperations.Clear();
            _bulkOperationMetadata.Clear();
            _currentBulkSize = 0;
            return [];
        }
        catch (MongoBulkWriteException ex)
        {
            var failedPartitions = new List<FailedPartition>();

            foreach (var writeError in ex.WriteErrors)
            {
                if (_bulkOperationMetadata.TryGetValue(writeError.Index, out var metadata))
                {
                    failedPartitions.Add(new FailedPartition(metadata.EventSourceId, metadata.SequenceNumber));
                }
            }

            _bulkOperations.Clear();
            _bulkOperationMetadata.Clear();
            _currentBulkSize = 0;

            return failedPartitions;
        }
    }

    static int EstimateOperationSize(WriteModel<BsonDocument> operation)
    {
        // Rough estimate: most operations are less than 10KB
        // For more accurate sizing, we could serialize the operation, but that's expensive
        // These values are conservative estimates based on typical document sizes
        const int EstimatedUpdateSize = 5000;   // Typical update operations with nested documents
        const int EstimatedDeleteSize = 500;    // Delete operations are much smaller
        const int DefaultEstimatedSize = 1024;  // Fallback for unknown operation types

        return operation switch
        {
            UpdateOneModel<BsonDocument> => EstimatedUpdateSize,
            DeleteOneModel<BsonDocument> => EstimatedDeleteSize,
            _ => DefaultEstimatedSize
        };
    }

    async Task RemoveChildFromAll(Key key, ChildRemovedFromAll childRemoved)
    {
        var childrenProperty = (string)childRemoved.ChildrenProperty.GetChildrenProperty();
        var identifiedByProperty = (string)childRemoved.IdentifiedByProperty;
        var propertyValue = key.Value.ToBsonValue();

        var collection = Collection;

        var filter = Builders<BsonDocument>.Filter.Empty;
        var childFilter = Builders<BsonDocument>.Filter.Eq(identifiedByProperty, propertyValue);
        var update = Builders<BsonDocument>.Update.PullFilter(childrenProperty, childFilter);
        await collection.UpdateManyAsync(filter, update);
    }

    IMongoCollection<BsonDocument> Collection => collections.GetCollection();
}
