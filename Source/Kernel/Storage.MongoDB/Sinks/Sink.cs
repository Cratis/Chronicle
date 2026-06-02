// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.ReadModels;
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

    readonly object _bulkLock = new();
    readonly List<WriteModel<BsonDocument>> _bulkOperations = [];
    readonly Dictionary<int, (Key EventSourceId, EventSequenceNumber SequenceNumber)> _bulkOperationMetadata = [];
    readonly ConcurrentDictionary<string, ExpandoObject> _bulkStateCache = new();
    readonly ConcurrentDictionary<string, Key> _bulkKeysByCacheKey = new();
    int _currentBulkSize;
    volatile bool _isBulkMode;

    /// <inheritdoc/>
    public SinkTypeName Name => "MongoDB";

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.MongoDB;

    /// <inheritdoc/>
    public async Task<ExpandoObject?> FindOrDefault(Key key)
    {
        if (_isBulkMode)
        {
            var cacheKey = converter.ToBsonValue(key).ToString()!;
            if (_bulkStateCache.TryGetValue(cacheKey, out var cachedState))
            {
                return cachedState;
            }
        }

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
        var hasDirectKeyScopedChanges = changeset.Changes.Any(change =>
            change is PropertiesChanged<ExpandoObject> or ChildAdded or ChildRemoved);
        var keyFilterValue = converter.ToBsonValue(key);

        var filter = changeset.HasJoined() && !hasDirectKeyScopedChanges ?
            FilterDefinition<BsonDocument>.Empty :
            Builders<BsonDocument>.Filter.Eq("_id", keyFilterValue);

        if (changeset.HasBeenRemoved())
        {
            if (_isBulkMode)
            {
                AddToBulk(new DeleteOneModel<BsonDocument>(filter), key, eventSequenceNumber);
                var cacheKey = converter.ToBsonValue(key).ToString()!;
                _bulkStateCache.TryRemove(cacheKey, out _);
                _bulkKeysByCacheKey.TryRemove(cacheKey, out _);
                return await FlushBulkIfNeeded();
            }

            await Collection.DeleteOneAsync(filter);
            return [];
        }

        // Run through and remove all children affected by ChildRemovedFromAll
        foreach (var childRemoved in changeset.Changes.OfType<ChildRemovedFromAll>())
        {
            await RemoveChildFromAll(childRemoved);
        }

        // For join events in bulk mode, flush pending operations first so that the join
        // can read committed data. Skip the Count check outside the lock to avoid reading
        // the list without synchronization.
        if (_isBulkMode && changeset.HasJoined())
        {
            await ExecuteBulk();
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
            if (!changeset.HasJoined())
            {
                var cacheKey = converter.ToBsonValue(key).ToString()!;
                _bulkStateCache[cacheKey] = changeset.CurrentState;
                _bulkKeysByCacheKey[cacheKey] = key;
            }

            if (changeset.HasJoined())
            {
                return await ExecuteBulk();
            }

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
        lock (_bulkLock)
        {
            _isBulkMode = true;
            _bulkOperations.Clear();
            _bulkOperationMetadata.Clear();
            _currentBulkSize = 0;
        }

        _bulkStateCache.Clear();
        _bulkKeysByCacheKey.Clear();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task EndBulk()
    {
        await ExecuteBulk();
        lock (_bulkLock)
        {
            _isBulkMode = false;
            _bulkOperations.Clear();
            _bulkOperationMetadata.Clear();
            _currentBulkSize = 0;
        }

        _bulkStateCache.Clear();
        _bulkKeysByCacheKey.Clear();
    }

    /// <inheritdoc/>
    public Task PrepareInitialRun() => collections.PrepareInitialRun();

    /// <inheritdoc/>
    public async Task BeginReplay(ReplayContext context)
    {
        await collections.BeginReplay(context);
        await BeginBulk();
    }

    /// <inheritdoc/>
    public async Task ResumeReplay(ReplayContext context)
    {
        await collections.ResumeReplay(context);
        await BeginBulk();
    }

    /// <inheritdoc/>
    public async Task EndReplay(ReplayContext context)
    {
        await EndBulk();
        await collections.EndReplay(context);
    }

    /// <inheritdoc/>
    public Task Remove(ReadModelContainerName containerName) => collections.Remove(containerName);

    /// <inheritdoc/>
    public async Task<Option<Key>> TryFindRootKeyByChildValue(PropertyPath childPropertyPath, object childValue)
    {
        if (_isBulkMode)
        {
            var pathSegments = childPropertyPath.Segments.ToArray();
            foreach (var (cacheKey, cachedState) in _bulkStateCache)
            {
                if (TryFindValueInDocument(cachedState, pathSegments, 0, childValue) &&
                    _bulkKeysByCacheKey.TryGetValue(cacheKey, out var rootKey))
                {
                    return new Option<Key>(rootKey);
                }
            }
        }

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

    /// <inheritdoc/>
    public async Task<ReadModelInstances> GetInstances(ReadModelContainerName? occurrence = null, int skip = 0, int take = 50)
    {
        var collection = occurrence is not null ? collections.GetCollection(occurrence) : Collection;
        var totalCount = await collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        var documents = await collection
            .Find(FilterDefinition<BsonDocument>.Empty)
            .Skip(skip)
            .Limit(take)
            .ToListAsync();

        var instances = documents.Select(doc => expandoObjectConverter.ToExpandoObject(doc, readModel.GetSchemaForLatestGeneration()));
        return new ReadModelInstances(instances, totalCount);
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
        lock (_bulkLock)
        {
            var operationIndex = _bulkOperations.Count;
            _bulkOperations.Add(operation);
            _bulkOperationMetadata[operationIndex] = (key, eventSequenceNumber);
            _currentBulkSize += EstimateOperationSize(operation);
        }
    }

    async Task<IEnumerable<FailedPartition>> FlushBulkIfNeeded()
    {
        bool shouldFlush;
        lock (_bulkLock)
        {
            shouldFlush = _bulkOperations.Count >= MaxBulkOperations || _currentBulkSize >= MaxBulkSizeInBytes;
        }

        if (shouldFlush)
        {
            return await ExecuteBulk();
        }

        return [];
    }

    async Task<IEnumerable<FailedPartition>> ExecuteBulk()
    {
        List<WriteModel<BsonDocument>> snapshot;
        Dictionary<int, (Key EventSourceId, EventSequenceNumber SequenceNumber)> metadataSnapshot;

        lock (_bulkLock)
        {
            if (_bulkOperations.Count == 0)
            {
                return [];
            }

            snapshot = [.._bulkOperations];
            metadataSnapshot = new(_bulkOperationMetadata);
            _bulkOperations.Clear();
            _bulkOperationMetadata.Clear();
            _currentBulkSize = 0;
        }

        try
        {
            await Collection.BulkWriteAsync(snapshot);
            return [];
        }
        catch (MongoBulkWriteException ex)
        {
            var failedPartitions = new List<FailedPartition>();

            foreach (var writeError in ex.WriteErrors)
            {
                if (metadataSnapshot.TryGetValue(writeError.Index, out var metadata))
                {
                    failedPartitions.Add(new FailedPartition(metadata.EventSourceId, metadata.SequenceNumber));
                }
            }

            return failedPartitions;
        }
    }

    bool TryFindValueInDocument(ExpandoObject document, IPropertyPathSegment[] pathSegments, int segmentIndex, object targetValue)
    {
        if (segmentIndex >= pathSegments.Length)
        {
            return false;
        }

        var currentSegment = pathSegments[segmentIndex];
        var dict = (IDictionary<string, object?>)document;

        if (!dict.TryGetValue(currentSegment.Value, out var value) || value is null)
        {
            return false;
        }

        if (segmentIndex == pathSegments.Length - 1)
        {
            return ValuesAreEqual(value, targetValue);
        }

        if (value is IEnumerable<object> collection)
        {
            foreach (var itemExpando in collection.OfType<ExpandoObject>())
            {
                if (TryFindValueInDocument(itemExpando, pathSegments, segmentIndex + 1, targetValue))
                {
                    return true;
                }
            }
        }
        else if (value is ExpandoObject nestedExpando)
        {
            return TryFindValueInDocument(nestedExpando, pathSegments, segmentIndex + 1, targetValue);
        }

        return false;
    }

    static bool ValuesAreEqual(object value, object targetValue)
    {
        if (value.Equals(targetValue))
        {
            return true;
        }

        return value.ToString() == targetValue.ToString();
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

    async Task RemoveChildFromAll(ChildRemovedFromAll childRemoved)
    {
        var childrenProperty = (string)childRemoved.ChildrenProperty.GetChildrenProperty();
        var identifiedByProperty = (string)childRemoved.IdentifiedByProperty;
        var propertyValue = childRemoved.Key.ToBsonValue();

        var collection = Collection;

        var filter = Builders<BsonDocument>.Filter.Empty;
        var childFilter = Builders<BsonDocument>.Filter.Eq(identifiedByProperty, propertyValue);
        var update = Builders<BsonDocument>.Update.PullFilter(childrenProperty, childFilter);
        await collection.UpdateManyAsync(filter, update);
    }

    IMongoCollection<BsonDocument> Collection => collections.GetCollection();
}
