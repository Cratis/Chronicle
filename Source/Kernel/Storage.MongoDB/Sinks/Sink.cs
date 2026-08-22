// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Dynamic;
using System.Reactive.Linq;
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

    /// <summary>
    /// Highest event sequence number known to be applied to each document while a bulk window is open.
    /// </summary>
    /// <remarks>
    /// Bulk mode answers <see cref="FindOrDefault"/> from <see cref="_bulkStateCache"/>, so a guarded write that
    /// the server rejects must not leave its recomputed state in that cache — the next event for the same key
    /// would read the doubled state and persist it. Seeded from the document the first uncached
    /// <see cref="FindOrDefault"/> reads (no extra round trip) and advanced by every write queued in the window,
    /// so an already applied event is recognized before its state is cached at all.
    /// </remarks>
    readonly ConcurrentDictionary<string, ulong> _bulkWatermarks = new();

    /// <summary>
    /// Documents whose delete is queued in the open bulk window but has not reached the server yet.
    /// </summary>
    /// <remarks>
    /// The server still holds such a document, so an uncached <see cref="FindOrDefault"/> would report it as
    /// present and the caller would treat the next event as an update of a live instance. On a document whose
    /// write is guarded that is fatal: a guarded write never inserts, so the queued delete runs first and the
    /// re-creating update matches nothing, losing the read model. Reporting the document as already gone restores
    /// the unguarded, upserting write that re-creates it — and incidentally stops the caller merging onto state
    /// that a queued delete has logically discarded.
    /// </remarks>
    readonly ConcurrentDictionary<string, byte> _bulkPendingDeletes = new();
    int _currentBulkSize;
    volatile bool _isBulkMode;

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

            if (_bulkPendingDeletes.ContainsKey(cacheKey))
            {
                return default;
            }
        }

        var collection = Collection;

        using var result = await collection.FindAsync(Builders<BsonDocument>.Filter.Eq("_id", converter.ToBsonValue(key)));
        var instance = result.SingleOrDefault();
        if (instance != default)
        {
            if (_isBulkMode &&
                instance.TryGetValue(WellKnownProperties.LastHandledEventSequenceNumber, out var watermark) &&
                watermark.IsNumeric)
            {
                RecordBulkWatermark(converter.ToBsonValue(key).ToString()!, (ulong)watermark.ToInt64());
            }

            return expandoObjectConverter.ToExpandoObject(instance, readModel.GetSchemaForLatestGeneration());
        }

        return default;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> ApplyChanges(
        Key key,
        IChangeset<AppendedEvent, ExpandoObject> changeset,
        EventSequenceNumber eventSequenceNumber) =>
        ApplyChanges(key, changeset, eventSequenceNumber, SinkWriteMode.Always);

    /// <inheritdoc/>
    public async Task<IEnumerable<FailedPartition>> ApplyChanges(
        Key key,
        IChangeset<AppendedEvent, ExpandoObject> changeset,
        EventSequenceNumber eventSequenceNumber,
        SinkWriteMode mode)
    {
        var hasDirectKeyScopedChanges = changeset.Changes.Any(change =>
            change is PropertiesChanged<ExpandoObject> or ChildAdded or ChildRemoved);
        var hasConstructiveChanges = changeset.Changes.Any(change =>
            change is ChildAdded or ChildRemoved);

        // When the event was consumed by a Join (Children Join<TEvent>) AND the only direct
        // key-scoped changes are PropertiesChanged (no ChildAdded / ChildRemoved that would
        // legitimately construct a document at this key), the upsert keyed on the join value
        // would create a phantom document. The classic case: a Group projection with
        // FromEvery.Set(LastUpdated) + Children.Join<UserCreated>. When UserCreated arrives
        // for a UserId that no Group has, the FromEvery PropertiesChanged would otherwise
        // upsert a phantom Group keyed on UserId. Use the join-targets-only filter (Empty)
        // in this case so only existing documents are updated.
        var hasJoined = changeset.HasJoined();
        var hasActualRootLevelJoin = HasActualRootLevelJoin(changeset.Changes);
        var onlyPropertyUpdatesAlongsideJoin = hasJoined && hasDirectKeyScopedChanges && !hasConstructiveChanges;
        var shouldSuppressRootUpdateAfterRootLevelJoin = hasActualRootLevelJoin && onlyPropertyUpdatesAlongsideJoin;

        // Compute the _id filter value only when the document is actually keyed by _id. For a join whose
        // target documents are matched by the join column (the Empty filter below), the resolved key carries
        // the JOIN VALUE — which for a differently-typed read model key (e.g. a string organization number
        // against a Guid-keyed model) cannot be converted to the _id type and throws "Unrecognized Guid
        // format", freezing the partition — even though that value is never used to key a document here.
        var usesJoinTargetsOnlyFilter = (hasJoined && !hasDirectKeyScopedChanges) || onlyPropertyUpdatesAlongsideJoin;
        var filter = usesJoinTargetsOnlyFilter ?
            FilterDefinition<BsonDocument>.Empty :
            Builders<BsonDocument>.Filter.Eq("_id", converter.ToBsonValue(key));

        // A ROOT-level join (no array indexers) never CONSTRUCTS a root document — it only enriches an
        // existing root matched by the join column; the root's own key is set by its From/[FromEvent] source.
        // Upserting on the resolved key for a root join would materialize a phantom root keyed by the JOIN
        // VALUE, which for a differently-typed read model key (e.g. a string organization number against a
        // Guid-keyed model) is stored with a string _id and freezes the partition the moment a later read
        // coerces it back to the key type ("Unrecognized Guid format"). A CHILD join (has array indexers)
        // still upserts so it can construct the child structure regardless of seed order.
        var isRootLevelJoin = hasJoined && !key.ArrayIndexers.All.Any();
        var isUpsert = !onlyPropertyUpdatesAlongsideJoin && !isRootLevelJoin;

        if (changeset.HasBeenRemoved())
        {
            if (_isBulkMode)
            {
                AddToBulk(new DeleteOneModel<BsonDocument>(filter), key, eventSequenceNumber);
                var cacheKey = converter.ToBsonValue(key).ToString()!;
                _bulkStateCache.TryRemove(cacheKey, out _);
                _bulkKeysByCacheKey.TryRemove(cacheKey, out _);
                _bulkWatermarks.TryRemove(cacheKey, out _);

                // Marked after the operation is queued, never before: a flush that observes the mark without the
                // operation would clear it while the delete is still pending, which is the failure this prevents.
                _bulkPendingDeletes[cacheKey] = 0;
                return await FlushBulkIfNeeded();
            }

            await Collection.DeleteOneAsync(filter);
            return [];
        }

        // A guarded write is a conditional UPDATE of a document the caller already observed, never an insert:
        // narrowing the filter to documents whose watermark is behind this event turns a crash-recovery
        // redelivery into a no-op. Upsert is switched off because a filter that matches nothing would otherwise
        // attempt an insert on an _id that already exists, which raises a duplicate key error and — inside an
        // ordered bulk write — would discard every operation queued behind it.
        if (mode == SinkWriteMode.OnlyWhenAdvancingWatermark && !usesJoinTargetsOnlyFilter && eventSequenceNumber.IsActualValue)
        {
            if (_isBulkMode &&
                _bulkWatermarks.TryGetValue(converter.ToBsonValue(key).ToString()!, out var applied) &&
                applied >= eventSequenceNumber.Value)
            {
                return [];
            }

            filter = Builders<BsonDocument>.Filter.And(filter, BelowWatermark(eventSequenceNumber));
            isUpsert = false;
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

        // ChangesetConverter has already executed the correctly filtered UpdateMany for the root join.
        // Any remaining direct root PropertiesChanged have no single _id target, so issuing the follow-up
        // UpdateOne would pick an arbitrary document (Filter.Empty) and corrupt it.
        if (shouldSuppressRootUpdateAfterRootLevelJoin)
        {
            return [];
        }

        if (_isBulkMode)
        {
            var updateModel = new UpdateOneModel<BsonDocument>(filter, converted.UpdateDefinition)
            {
                IsUpsert = isUpsert,
                ArrayFilters = converted.ArrayFilters
            };
            AddToBulk(updateModel, key, eventSequenceNumber);
            if (!changeset.HasJoined())
            {
                var cacheKey = converter.ToBsonValue(key).ToString()!;
                _bulkStateCache[cacheKey] = changeset.CurrentState;
                _bulkKeysByCacheKey[cacheKey] = key;
                _bulkPendingDeletes.TryRemove(cacheKey, out _);
                if (eventSequenceNumber.IsActualValue)
                {
                    RecordBulkWatermark(cacheKey, eventSequenceNumber.Value);
                }
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
                IsUpsert = isUpsert,
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
        _bulkWatermarks.Clear();
        _bulkPendingDeletes.Clear();
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
        _bulkWatermarks.Clear();
        _bulkPendingDeletes.Clear();
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

        // Deliberately NOT converted through the schema, unlike the join filter in ChangesetConverter. The two
        // look like the same defect and are not, because this lookup only ever RESOLVES A KEY and the join write
        // no longer depends on it having found one:
        //
        // - For a root-level join this is asked with the read model's own key property and the join source's raw
        //   event source id, so on a Guid-keyed model it compares a string against BinData and misses. Making it
        //   match changes the resolved key from that string to the root's typed _id, and Joined.Key is derived
        //   from the resolved key - so a join declared on a STRING column (the shadow-column shape a consumer
        //   adopts precisely because their id is Guid-backed) would start comparing BinData against a string and
        //   stop matching. A correct fix therefore has to carry the original value through as ResolvedKey.JoinKey
        //   at the same time; the two changes are not separable.
        // - The bulk branch above answers the same question by CLR equality over cached state, and the SQL and
        //   in-memory sinks answer it differently again - SQL only for a JSON column, in-memory by CLR value.
        //   Converting here alone makes one sink resolve a key the other three do not, which is the divergence
        //   the framework rules single out as worse than the miss.
        //
        // What it would take: JoinKey propagation in KeyResolvers.ForJoin, the same conversion in the bulk
        // branch, a parity pass over the SQL and in-memory sinks, and specs covering a root-level join on a
        // string column against a Guid-keyed read model in every one of them.
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

    /// <inheritdoc/>
    public IObservable<IEnumerable<ExpandoObject>> ObserveInstances(ReadModelContainerName? occurrence = null, int skip = 0, int take = 50)
    {
        var collection = occurrence is not null ? collections.GetCollection(occurrence) : Collection;
        var schema = readModel.GetSchemaForLatestGeneration();

        // Return an observable that transforms MongoDB change stream events into instance collections
        return Observable.Create<IEnumerable<ExpandoObject>>(async observer =>
        {
            // Get initial instances
            var documents = await collection
                .Find(FilterDefinition<BsonDocument>.Empty)
                .Skip(skip)
                .Limit(take)
                .ToListAsync();

            observer.OnNext(documents.Select(doc => expandoObjectConverter.ToExpandoObject(doc, schema)));

            // Subscribe to changes using Arc's Observe extension
            return collection.Observe().Subscribe(
                allDocuments =>
                {
                    // Re-query with skip/take when changes occur
                    var updatedDocuments = allDocuments.Skip(skip).Take(take);
                    observer.OnNext(updatedDocuments.Select(doc => expandoObjectConverter.ToExpandoObject(doc, schema)));
                },
                observer.OnError,
                observer.OnCompleted);
        });
    }

    static bool HasActualRootLevelJoin(IEnumerable<Change> changes) =>
        changes
            .OfType<Joined>()
            .Any(joined => !joined.ArrayIndexers.All.Any());

    /// <summary>
    /// Builds the clause that restricts a write to documents that have not yet observed the given event.
    /// </summary>
    /// <param name="eventSequenceNumber">The <see cref="EventSequenceNumber"/> about to be applied.</param>
    /// <returns>The <see cref="FilterDefinition{TDocument}"/> matching documents behind the watermark.</returns>
    /// <remarks>
    /// Documents written before the watermark property existed carry no value at all, which the
    /// <c>$exists</c> clause admits so they establish it on their first guarded write.
    /// </remarks>
    FilterDefinition<BsonDocument> BelowWatermark(EventSequenceNumber eventSequenceNumber) =>
        Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Exists(WellKnownProperties.LastHandledEventSequenceNumber, false),
            Builders<BsonDocument>.Filter.Lt(WellKnownProperties.LastHandledEventSequenceNumber, converter.ToBsonValue(eventSequenceNumber)));

    void RecordBulkWatermark(string cacheKey, ulong eventSequenceNumber) =>
        _bulkWatermarks.AddOrUpdate(
            cacheKey,
            static (_, incoming) => incoming,
            static (_, current, incoming) => Math.Max(current, incoming),
            eventSequenceNumber);

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
        string[] flushedPendingDeletes;

        lock (_bulkLock)
        {
            if (_bulkOperations.Count == 0)
            {
                return [];
            }

            snapshot = [.._bulkOperations];
            metadataSnapshot = new(_bulkOperationMetadata);

            // Only the marks that exist now can belong to operations in this snapshot; a mark added afterwards
            // belongs to a delete still queued and must survive the flush.
            flushedPendingDeletes = [.._bulkPendingDeletes.Keys];
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
        finally
        {
            foreach (var cacheKey in flushedPendingDeletes)
            {
                _bulkPendingDeletes.TryRemove(cacheKey, out _);
            }
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
