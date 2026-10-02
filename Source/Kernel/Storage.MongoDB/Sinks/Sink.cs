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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
/// <param name="changeStreams"><see cref="IReadModelChangeStreams"/> for observing the collections of the read model.</param>
/// <param name="logger"><see cref="ILogger{TCategoryName}"/> for logging.</param>
public class Sink(
    ReadModelDefinition readModel,
    IMongoDBConverter converter,
    ISinkCollections collections,
    IChangesetConverter changesetConverter,
    IExpandoObjectConverter expandoObjectConverter,
    IReadModelChangeStreams changeStreams,
    ILogger<Sink> logger) : ISink
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Sink"/> class without a logger.
    /// </summary>
    /// <param name="readModel">The <see cref="ReadModelDefinition"/> the sink is for.</param>
    /// <param name="converter"><see cref="IMongoDBConverter"/> for dealing with conversion.</param>
    /// <param name="collections">Provider for <see cref="ISinkCollections"/> to use.</param>
    /// <param name="changesetConverter">Provider for <see cref="IChangesetConverter"/> for converting changesets.</param>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between documents and <see cref="ExpandoObject"/>.</param>
    /// <param name="changeStreams"><see cref="IReadModelChangeStreams"/> for observing the collections of the read model.</param>
    /// <remarks>
    /// Retained so a caller written against the previous constructor keeps compiling; it forgoes the diagnostics
    /// naming the MongoDB errors behind a failed bulk write.
    /// </remarks>
    public Sink(
        ReadModelDefinition readModel,
        IMongoDBConverter converter,
        ISinkCollections collections,
        IChangesetConverter changesetConverter,
        IExpandoObjectConverter expandoObjectConverter,
        IReadModelChangeStreams changeStreams)
        : this(readModel, converter, collections, changesetConverter, expandoObjectConverter, changeStreams, NullLogger<Sink>.Instance)
    {
    }

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
        var hasKeyedFrom = changeset.Changes.OfType<Joined>().Any(joined => joined.HasKeyedFrom);

        // A From and a Join can consume the same event. Only a From owns the direct keyed write;
        // a join-only event's direct properties have no safe _id target and must still be suppressed.
        var onlyPropertyUpdatesAlongsideJoin = hasJoined && hasDirectKeyScopedChanges && !hasConstructiveChanges && !hasKeyedFrom;
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
        var isUpsert = !onlyPropertyUpdatesAlongsideJoin && (!isRootLevelJoin || hasKeyedFrom);

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

        // Reads omit BSON nulls, so a missing parent in the initial state can still be
        // present as null in an older document. Unset only that legacy null (not an
        // existing object) before dotted leaf sets. Keep these ordered with the main
        // write in bulk mode, and preserve the watermark guard on redelivery.
        if (!usesJoinTargetsOnlyFilter)
        {
            foreach (var parent in converted.NullParentPaths.Select(path => (Path: path, Filters: (IReadOnlyList<BsonDocumentArrayFilterDefinition<BsonDocument>>)[]))
                         .Concat(converted.NullArrayParents.Select(_ => (_.Path, Filters: _.ArrayFilters)))
                         .OrderBy(_ => _.Path.Count(ch => ch == '.')))
            {
                var nullFilter = parent.Filters.Count == 0
                    ? Builders<BsonDocument>.Filter.And(filter, Builders<BsonDocument>.Filter.Type(parent.Path, BsonType.Null))
                    : filter;
                var unset = Builders<BsonDocument>.Update.Unset(parent.Path);
                if (_isBulkMode)
                {
                    AddToBulk(new UpdateOneModel<BsonDocument>(nullFilter, unset) { ArrayFilters = parent.Filters }, key, eventSequenceNumber);
                }
                else
                {
                    await Collection.UpdateOneAsync(nullFilter, unset, new UpdateOptions { ArrayFilters = parent.Filters });
                }
            }
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
    public async Task<IEnumerable<FailedPartition>> EndBulk()
    {
        // Bulk mode ends whatever the final flush does: a sink left in bulk mode would hold every later
        // write back until a thousand of them had queued up.
        try
        {
            return await ExecuteBulk();
        }
        finally
        {
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
    }

    /// <inheritdoc/>
    public Task PrepareInitialRun() => collections.PrepareInitialRun();

    /// <inheritdoc/>
    public async Task BeginReplay(ReplayContext context)
    {
        await collections.BeginReplay(context);

        // A replay writes into its own shadow collection and that collection is renamed into place at the end,
        // taking its own indexes with it - and only its own. Indexes were ensured once when the sink was built,
        // against the collection that the swap replaces, so without this the promoted collection comes up with
        // none of them until something rebuilds the sink. Recreating them is exactly what the declaration on the
        // read model is for (#3942).
        await EnsureIndexes();
        await BeginBulk();
    }

    /// <inheritdoc/>
    public async Task ResumeReplay(ReplayContext context)
    {
        await collections.ResumeReplay(context);
        await EnsureIndexes();
        await BeginBulk();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Ending a replay always takes the sink out of replay mode. The sink is cached and shared by every later write on
    /// this silo, so one left in replay mode would keep sending live changes to the replay collection, which nothing
    /// reads, until the process restarts.
    /// <para>
    /// Partitions that failed in the final flush do not hold back the promotion. They are returned so the observer
    /// records them and retries them from the event that failed - against the promoted collection, which holds every
    /// earlier event for the partition. Not promoting would leave the observer continuing from the end of the replay
    /// on top of the collection as it was before the replay began; another silo ending the same replay without
    /// failures would promote it regardless. Only a final flush that throws, leaving the outcome unknown and nothing
    /// recorded, abandons the rebuilt collection instead.
    /// </para>
    /// </remarks>
    public async Task<IEnumerable<FailedPartition>> EndReplay(ReplayContext context)
    {
        FailedPartition[] failedPartitions;
        try
        {
            failedPartitions = (await EndBulk()).ToArray();
        }
        catch
        {
            logger.AbandoningReplayAfterFailedFlush(readModel.Identifier);
            collections.AbandonReplay();
            throw;
        }

        if (failedPartitions.Length > 0)
        {
            logger.EndingReplayWithFailedPartitions(readModel.Identifier, failedPartitions.Length);
        }

        await collections.EndReplay(context);
        return failedPartitions;
    }

    /// <inheritdoc/>
    public async Task LeaveReplay()
    {
        // Whatever is still held back belongs to a replay that has been promoted without it. Writing it to the replay
        // collection would lose it, so it goes to the read model's own collection once replay mode is left.
        collections.AbandonReplay();
        await EndBulk();
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
        using var cursor = await collection.Indexes.ListAsync();
        var existingIndexes = await cursor.ToListAsync();
        BsonValue? collation = null;

        foreach (var indexDefinition in readModel.Indexes)
        {
            var indexName = $"chronicle_idx_{indexDefinition.PropertyPath.Path.Replace('.', '_')}";

            var candidates = existingIndexes.FindAll(index => HasMatchingKeyAndOptions(index, indexDefinition.PropertyPath.Path));
            if (candidates.Exists(index => index.GetValue("name", BsonNull.Value) == indexName))
            {
                continue;
            }

            if (candidates.Count > 0)
            {
                collation ??= await GetCollectionCollation(collection);
                if (candidates.Exists(index => index.GetValue("collation", BsonNull.Value).Equals(collation)))
                {
                    continue;
                }
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
        // Resolved exactly like ObserveInstances, so the count and the page of an observed response come from the same collection.
        var collection = collections.GetCollection(occurrence ?? readModel.ContainerName);
        var totalCount = await collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        var instances = await ReadPage(collection, skip, take, CancellationToken.None);
        return new ReadModelInstances(instances, totalCount);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Changes are observed through a MongoDB change stream shared by every observer of the collection (see
    /// <see cref="IReadModelChangeStreams"/>), so a write made by any silo is seen, not only one made through this
    /// sink instance. Every batch of changes re-reads the page once, so a burst of writes costs one query rather
    /// than one per write.
    /// <para>
    /// Without an explicit occurrence the primary collection is observed, even while a replay is in progress:
    /// the replay rebuilds its own collection, and the primary one keeps the previous state until the replay
    /// ends and promotes the rebuilt collection - which the stream sees as a rename and answers with a fresh page.
    /// </para>
    /// <para>
    /// A replay promotion renames the primary collection aside and then renames the rebuilt one into its place, so a
    /// read that runs inside that window would find no collection and report an empty read model. A promotion is in
    /// progress only while the primary collection is absent and the promoting collection exists. Observing the primary
    /// collection therefore looks at the collections before and after an empty read:
    /// no page is emitted when a promotion is in progress before the read - the read is skipped - or when one is in
    /// progress after an empty read, or when the primary collection is not the one that was there before the read (it
    /// was renamed or replaced in between). The rename into place is what triggers the read that follows. A collection
    /// that has never been created, has been dropped or is genuinely empty is an empty read model, and emits the empty
    /// page. The checks are one listCollections command before every read and one more after every empty page.
    /// Observing an explicit occurrence never holds a page back.
    /// </para>
    /// <para>
    /// The promoting collection alone does not mean a promotion is in progress: a silo that dies between claiming the
    /// replay collection and renaming it into place, or a rename that fails, leaves it behind until the next replay
    /// begins. Next to an existing primary collection it is ignored, and reads and emissions proceed normally. If such
    /// a crash left the primary collection absent as well, the next write recreates it and observation resumes.
    /// </para>
    /// </remarks>
    public IObservable<IEnumerable<ExpandoObject>> ObserveInstances(ReadModelContainerName? occurrence = null, int skip = 0, int take = 50)
    {
        string containerName = occurrence ?? readModel.ContainerName;
        var collection = collections.GetCollection(containerName);
        var isPrimary = occurrence is null;
        return changeStreams
            .Observe(
                collection.Database,
                containerName,
                cancellationToken => ReadObservedPage(collection, containerName, isPrimary, skip, take, cancellationToken))
            .Where(page => page is not null)
            .Select(page => page!);
    }

    async Task<IEnumerable<ExpandoObject>?> ReadObservedPage(IMongoCollection<BsonDocument> collection, string containerName, bool isPrimary, int skip, int take, CancellationToken cancellationToken)
    {
        if (!isPrimary)
        {
            return await ReadPage(collection, skip, take, cancellationToken);
        }

        var before = await GetCollectionState(collection.Database, containerName, cancellationToken);
        if (before.IsPromotionInProgress)
        {
            return null;
        }

        var page = await ReadPage(collection, skip, take, cancellationToken);
        if (page.Any())
        {
            return page;
        }

        var after = await GetCollectionState(collection.Database, containerName, cancellationToken);
        return after.IsPromotionInProgress || after.PrimaryExists != before.PrimaryExists || after.PrimaryId != before.PrimaryId ? null : page;
    }

    async Task<CollectionState> GetCollectionState(IMongoDatabase database, string containerName, CancellationToken cancellationToken)
    {
        var promotingName = collections.PromotingCollectionName;
        var options = new ListCollectionsOptions
        {
            Filter = new BsonDocument("name", new BsonDocument("$in", new BsonArray { containerName, promotingName }))
        };
        using var cursor = await database.ListCollectionsAsync(options, cancellationToken);
        var listed = await cursor.ToListAsync(cancellationToken);
        var primary = listed.FirstOrDefault(collection => collection["name"] == containerName);
        var promotingExists = listed.Exists(collection => collection["name"] == promotingName);
        var primaryId = primary?.GetValue("info", new BsonDocument()).AsBsonDocument.GetValue("uuid", BsonNull.Value);
        return new CollectionState(promotingExists, primary is not null, primaryId);
    }

    async Task<IEnumerable<ExpandoObject>> ReadPage(IMongoCollection<BsonDocument> collection, int skip, int take, CancellationToken cancellationToken)
    {
        var documents = await collection
            .Find(FilterDefinition<BsonDocument>.Empty)
            .Sort(Builders<BsonDocument>.Sort.Ascending("_id"))
            .Skip(skip)
            .Limit(take)
            .ToListAsync(cancellationToken);

        var schema = readModel.GetSchemaForLatestGeneration();
        return documents.Select(document => expandoObjectConverter.ToExpandoObject(document, schema)).ToArray();
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
    /// <c language="csharp">$exists</c> clause admits so they establish it on their first guarded write.
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

    static bool HasMatchingKeyAndOptions(BsonDocument index, string propertyPath) =>
        index.GetValue("key", BsonNull.Value).Equals(new BsonDocument(propertyPath, 1)) &&
        index.GetValue("unique", false) == false &&
        index.GetValue("prepareUnique", false) == false &&
        index.GetValue("sparse", false) == false &&
        index.GetValue("hidden", false) == false &&
        !index.Contains("partialFilterExpression") &&
        !index.Contains("expireAfterSeconds");

    async Task<BsonValue> GetCollectionCollation(IMongoCollection<BsonDocument> collection)
    {
        // An index without an explicit collation inherits the collection's default. Compare that effective
        // collation as well as the key, so an index for different string comparisons is not mistaken for ours.
        using var cursor = await collection.Database.ListCollectionsAsync(new ListCollectionsOptions
        {
            Filter = new BsonDocument("name", collection.CollectionNamespace.CollectionName)
        });
        var definitions = await cursor.ToListAsync();
        return definitions.FirstOrDefault()?
            .GetValue("options", new BsonDocument()).AsBsonDocument
            .GetValue("collation", BsonNull.Value) ?? BsonNull.Value;
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

        var failedPartitions = new Dictionary<Key, (EventSequenceNumber SequenceNumber, string Reason)>();
        try
        {
            var remainingIndexes = Enumerable.Range(0, snapshot.Count).ToList();
            while (remainingIndexes.Count > 0)
            {
                var remaining = remainingIndexes.ConvertAll(index => snapshot[index]);
                try
                {
                    await Collection.BulkWriteAsync(remaining);
                    break;
                }
                catch (MongoBulkWriteException<BsonDocument> ex)
                {
                    var reasons = ReportBulkWriteErrors(ex, remainingIndexes, metadataSnapshot);

                    // ProcessedRequests includes the failed request, not just the successful writes.
                    // An ordered write can resume only when it identifies an exact processed prefix and
                    // an unprocessed suffix. A write concern error leaves the outcome uncertain.
                    if (ex.WriteConcernError is not null ||
                        ex.WriteErrors.Count != 1 ||
                        ex.Result?.IsAcknowledged != true ||
                        ex.WriteErrors[0].Index < 0 ||
                        ex.WriteErrors[0].Index >= remaining.Count ||
                        ex.Result.ProcessedRequests.Count != ex.WriteErrors[0].Index + 1 ||
                        !ex.Result.ProcessedRequests.SequenceEqual(remaining.Take(ex.Result.ProcessedRequests.Count)) ||
                        !ex.UnprocessedRequests.SequenceEqual(remaining.Skip(ex.Result.ProcessedRequests.Count)))
                    {
                        AddFailedPartitions(remainingIndexes, metadataSnapshot, failedPartitions, reasons);
                        break;
                    }

                    var failedOffset = ex.WriteErrors[0].Index;
                    AddFailedPartitions([remainingIndexes[failedOffset]], metadataSnapshot, failedPartitions, reasons);

                    // The observer will replay a failed partition from its earliest failed sequence number.
                    // Do not write any later changes for that partition ahead of the replayed change.
                    remainingIndexes = remainingIndexes.Skip(failedOffset + 1)
                        .Where(index => !metadataSnapshot.TryGetValue(index, out var metadata) ||
                            !failedPartitions.ContainsKey(metadata.EventSourceId))
                        .ToList();
                }
                catch (MongoBulkWriteException ex)
                {
                    var reasons = ReportBulkWriteErrors(ex, remainingIndexes, metadataSnapshot);
                    AddFailedPartitions(remainingIndexes, metadataSnapshot, failedPartitions, reasons);
                    break;
                }
            }

            return failedPartitions
                .Select(partition => new FailedPartition(partition.Key, partition.Value.SequenceNumber) { Reason = partition.Value.Reason })
                .ToArray();
        }
        finally
        {
            foreach (var cacheKey in flushedPendingDeletes)
            {
                _bulkPendingDeletes.TryRemove(cacheKey, out _);
            }
        }
    }

    static void AddFailedPartitions(
        IEnumerable<int> indexes,
        Dictionary<int, (Key EventSourceId, EventSequenceNumber SequenceNumber)> metadata,
        Dictionary<Key, (EventSequenceNumber SequenceNumber, string Reason)> failedPartitions,
        BulkWriteFailureReasons reasons)
    {
        foreach (var index in indexes)
        {
            if (metadata.TryGetValue(index, out var operationMetadata) &&
                (!failedPartitions.TryGetValue(operationMetadata.EventSourceId, out var earliest) ||
                    operationMetadata.SequenceNumber.Value < earliest.SequenceNumber.Value))
            {
                failedPartitions[operationMetadata.EventSourceId] = (operationMetadata.SequenceNumber, reasons.For(index));
            }
        }
    }

    /// <summary>
    /// Logs every error a bulk write reported and describes them so they can travel with the failed partitions.
    /// </summary>
    /// <param name="exception">The <see cref="MongoBulkWriteException"/> the write raised.</param>
    /// <param name="remainingIndexes">Indexes into the flushed batch of the operations that were sent, in the order they were sent.</param>
    /// <param name="metadata">The partition and event sequence number for each operation in the flushed batch.</param>
    /// <returns>The <see cref="BulkWriteFailureReasons"/> for the write.</returns>
    /// <remarks>
    /// Only codes and the server's messages are used. The error details document and the exception itself are left
    /// out, because both can echo the document being written.
    /// </remarks>
    BulkWriteFailureReasons ReportBulkWriteErrors(
        MongoBulkWriteException exception,
        List<int> remainingIndexes,
        Dictionary<int, (Key EventSourceId, EventSequenceNumber SequenceNumber)> metadata)
    {
        var collection = collections.GetCollection().CollectionNamespace?.CollectionName ?? readModel.ContainerName.Value;
        var byOperation = new Dictionary<int, string>();
        var first = string.Empty;

        foreach (var error in exception.WriteErrors)
        {
            var operationIndex = error.Index >= 0 && error.Index < remainingIndexes.Count ? remainingIndexes[error.Index] : -1;
            var partition = metadata.TryGetValue(operationIndex, out var operation) ? operation.EventSourceId.Value.ToString() ?? string.Empty : string.Empty;
            logger.BulkWriteErrorOccurred(collection, readModel.Identifier, partition, operationIndex, error.Code, error.Category, error.Message);

            var reason = $"MongoDB write error {error.Code} ({error.Category}): {error.Message}";
            byOperation.TryAdd(operationIndex, reason);
            if (first.Length == 0)
            {
                first = reason;
            }
        }

        if (exception.WriteConcernError is { } writeConcernError)
        {
            logger.BulkWriteConcernErrorOccurred(collection, readModel.Identifier, writeConcernError.Code, writeConcernError.CodeName, writeConcernError.Message, remainingIndexes.Count);
            if (first.Length == 0)
            {
                first = $"MongoDB write concern error {writeConcernError.Code} ({writeConcernError.CodeName}): {writeConcernError.Message}";
            }
        }

        return new(byOperation, first);
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

    /// <summary>
    /// The reasons a bulk write failed: the error for each operation the server rejected, and the first error
    /// reported, which stands in for operations that were not written because of it.
    /// </summary>
    /// <param name="ByOperation">The reason for each rejected operation, by its index in the flushed batch.</param>
    /// <param name="First">The first error the write reported.</param>
    sealed record BulkWriteFailureReasons(IReadOnlyDictionary<int, string> ByOperation, string First)
    {
        public string For(int operationIndex) => ByOperation.TryGetValue(operationIndex, out var reason) ? reason : First;
    }

    sealed record CollectionState(bool PromotingExists, bool PrimaryExists, BsonValue? PrimaryId)
    {
        /// <summary>
        /// Gets whether a promotion is in progress: only the window between renaming the primary aside and renaming the
        /// rebuilt collection into place. A promoting collection next to an existing primary is a leftover of a
        /// promotion that never finished.
        /// </summary>
        public bool IsPromotionInProgress => !PrimaryExists && PromotingExists;
    }
}
