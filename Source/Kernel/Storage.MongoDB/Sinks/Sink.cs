// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Dynamic;
using System.Reactive.Linq;
using System.Runtime.ExceptionServices;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Monads;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

#pragma warning disable CA1849, MA0042 // MongoDB breaks the Orleans task model internally, so it won't return to the task scheduler
#pragma warning disable SA1201, SA1202, SA1204 // Member ordering

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
/// <param name="writes"><see cref="ISinkWrites"/> executing the data writes, fenced or not.</param>
public partial class Sink(
    ReadModelDefinition readModel,
    IMongoDBConverter converter,
    ISinkCollections collections,
    IChangesetConverter changesetConverter,
    IExpandoObjectConverter expandoObjectConverter,
    IReadModelChangeStreams changeStreams,
    ILogger<Sink> logger,
    ISinkWrites writes) : ISink, IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Sink"/> class writing without a fence.
    /// </summary>
    /// <param name="readModel">The <see cref="ReadModelDefinition"/> the sink is for.</param>
    /// <param name="converter"><see cref="IMongoDBConverter"/> for dealing with conversion.</param>
    /// <param name="collections">Provider for <see cref="ISinkCollections"/> to use.</param>
    /// <param name="changesetConverter">Provider for <see cref="IChangesetConverter"/> for converting changesets.</param>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between documents and <see cref="ExpandoObject"/>.</param>
    /// <param name="changeStreams"><see cref="IReadModelChangeStreams"/> for observing the collections of the read model.</param>
    /// <param name="logger"><see cref="ILogger{TCategoryName}"/> for logging.</param>
    public Sink(
        ReadModelDefinition readModel,
        IMongoDBConverter converter,
        ISinkCollections collections,
        IChangesetConverter changesetConverter,
        IExpandoObjectConverter expandoObjectConverter,
        IReadModelChangeStreams changeStreams,
        ILogger<Sink> logger)
        : this(readModel, converter, collections, changesetConverter, expandoObjectConverter, changeStreams, logger, UnfencedSinkWrites.Instance)
    {
    }

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
    const int MaxPrimaryReadAttempts = 3;

    /// <summary>
    /// Maximum size in bytes for a bulk write operation.
    /// MongoDB's limit for bulk operations is 48MB, individual documents are limited to 16MB.
    /// </summary>
    const int MaxBulkSizeInBytes = 48 * 1024 * 1024;

    readonly object _bulkLock = new();
    readonly CancellationTokenSource _bulkCancellation = new();
    readonly List<WriteModel<BsonDocument>> _bulkOperations = [];
    readonly HashSet<BsonValue> _bulkDestinations = [];
    readonly List<BulkBatch> _inFlight = [];
    readonly Dictionary<BulkBatch, BulkOutcome> _unreportedBulkOutcomes = [];
    readonly Dictionary<int, (Key ReadModelKey, Key EventSourceId, EventSequenceNumber SequenceNumber)> _bulkOperationMetadata = [];
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
    readonly ConcurrentDictionary<string, long> _bulkPendingDeletes = new();
    long _pendingDeleteVersion;
    int _currentBulkSize;
    volatile bool _isBulkMode;
    volatile CollectionState? _primaryState;
    BulkWindowId? _bulkWindowId;
    bool _bulkWindowIsReplay;
    bool _bulkWindowHasFailedWrites;

    /// <summary>
    /// Completes when the bulk window that <see cref="EndBulk()"/> is closing has been flushed and left; set while it is.
    /// </summary>
    /// <remarks>
    /// Guarded by <see cref="_bulkLock"/>. While it is set, no write is admitted into the window: a write accepted after
    /// the final flush had taken its snapshot would otherwise be cleared without ever being written (#4598). Such a write
    /// waits for the closing to finish and is then written directly, after the flushed batch, so it cannot overtake it.
    /// </remarks>
    TaskCompletionSource? _closing;

    /// <summary>
    /// Gets the fixed-epoch, create-only index admission for an exclusively owned generation destination.
    /// </summary>
    /// <remarks>
    /// A delayed creation from a revoked epoch is benign while the destination exists, because the index set comes from the
    /// generation's immutable execution snapshot. Creating an index implicitly creates its collection, so whoever later drops a
    /// retired generation destination must first make sure no index creation for it can still arrive.
    /// </remarks>
    internal Func<Func<Task>, Task>? GenerationIndexDdl { get; init; }

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.MongoDB;

    /// <inheritdoc/>
    public void Dispose() => _bulkCancellation.Dispose();

    /// <inheritdoc/>
    public async Task<ExpandoObject?> FindOrDefault(Key key)
    {
        await SettleGuardedBulkReads();
        if (_isBulkMode && TryAnswerFromBulk(key, out var bulkAnswer))
        {
            return bulkAnswer;
        }

        var collection = Collection;

        using var result = await collection.FindAsync(Builders<BsonDocument>.Filter.Eq("_id", converter.ToBsonValue(key)));
        var instance = result.SingleOrDefault();
        if (instance != default)
        {
            SeedBulkWatermark(key, instance);
            return expandoObjectConverter.ToExpandoObject(instance, readModel.GetSchemaForLatestGeneration());
        }

        return default;
    }

    /// <inheritdoc/>
    /// <exception cref="ReadModelPrimaryIsChanging">Promotion prevents a reliable answer; the caller can retry.</exception>
    public async Task<ExpandoObject?> FindInPrimaryOrDefault(Key key)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await ReadPrimaryOrDefault(key);
            }
            catch (ReadModelPrimaryIsChanging)
            {
                _primaryState = null;
                if (attempt == MaxPrimaryReadAttempts - 1)
                {
                    logger.PrimaryReadAttemptsExhausted(readModel.Identifier, readModel.ContainerName, collections.PromotingCollectionName, MaxPrimaryReadAttempts);
                    throw;
                }

                await Task.Delay(attempt == 0 ? 20 : 50);
            }
        }
    }

    async Task<ExpandoObject?> ReadPrimaryOrDefault(Key key)
    {
        var collection = collections.GetCollection(readModel.ContainerName);

        // A hit under the primary name is complete even after a rename. Keep its identity for misses,
        // which must still detect a primary created, removed or replaced while the read was in flight.
        var before = _primaryState ??= await GetPrimaryState(collection.Database);

        using var result = await collection.FindAsync(Builders<BsonDocument>.Filter.Eq("_id", converter.ToBsonValue(key)));
        var instance = result.SingleOrDefault();
        if (instance is not null)
        {
            return expandoObjectConverter.ToExpandoObject(instance, readModel.GetSchemaForLatestGeneration());
        }

        var after = await GetPrimaryState(collection.Database);
        if (after.PrimaryExists != before.PrimaryExists || after.PrimaryId != before.PrimaryId)
        {
            throw new ReadModelPrimaryIsChanging(readModel.ContainerName);
        }

        return null;
    }

    async Task<CollectionState> GetPrimaryState(IMongoDatabase database)
    {
        var state = await GetCollectionState(database, readModel.ContainerName, CancellationToken.None);
        if (state.IsPromotionInProgress)
        {
            logger.PrimaryReadDuringPromotion(readModel.Identifier, readModel.ContainerName, collections.PromotingCollectionName);
            throw new ReadModelPrimaryIsChanging(readModel.ContainerName);
        }

        return state;
    }

    bool TryAnswerFromBulk(Key key, out ExpandoObject? answer)
    {
        var cacheKey = converter.ToBsonValue(key).ToString()!;
        if (_bulkStateCache.TryGetValue(cacheKey, out var cachedState))
        {
            answer = cachedState;
            return true;
        }

        answer = default;
        return _bulkPendingDeletes.ContainsKey(cacheKey);
    }

    void SeedBulkWatermark(Key key, BsonDocument instance)
    {
        if (!_isBulkMode)
        {
            return;
        }

        if (instance.TryGetValue(WellKnownProperties.LastHandledEventSequenceNumber, out var watermark) && watermark.IsNumeric)
        {
            RecordBulkWatermark(converter.ToBsonValue(key).ToString()!, (ulong)watermark.ToInt64());
        }
    }

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> ApplyChanges(
        Key key,
        IChangeset<AppendedEvent, ExpandoObject> changeset,
        EventSequenceNumber eventSequenceNumber) =>
        ApplyChanges(key, changeset, eventSequenceNumber, SinkWriteMode.Always);

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> ApplyChanges(
        Key key,
        IChangeset<AppendedEvent, ExpandoObject> changeset,
        EventSequenceNumber eventSequenceNumber,
        SinkWriteMode mode) => ApplyChangesCore(key, changeset, eventSequenceNumber, new SinkWriteOptions(mode));

    /// <inheritdoc/>
    public async Task<IEnumerable<FailedPartition>> ApplyChangesImmediately(
        Key key,
        IChangeset<AppendedEvent, ExpandoObject> changeset,
        EventSequenceNumber eventSequenceNumber,
        SinkWriteMode mode)
    {
        if (!_isBulkMode)
        {
            return await ApplyChanges(key, changeset, eventSequenceNumber, mode);
        }

        var drain = await DrainBulk();
        var drainedBatches = new HashSet<BulkBatch>(drain.Batches);
        var failures = new List<FailedPartition>();
        try
        {
            failures.AddRange(await ApplyChangesCore(key, changeset, eventSequenceNumber, new SinkWriteOptions(mode, drainedBatches)));
        }
        catch (MongoWriteException exception) when (exception.WriteError is not null && exception.WriteConcernError is null)
        {
            EvictBulkState(key);
            var error = exception.WriteError;
            failures.Add(new FailedPartition(PartitionFor(key, changeset), eventSequenceNumber) { Reason = $"MongoDB write error {error.Code} ({error.Category}): {error.Message}" });
        }

        // Claim only on a returning path. Exceptional exits leave the drained outcomes
        // in the ledger for the next drain or EndBulk.
        return TakeBulkFailures(drainedBatches).Concat(failures).ToArray();
    }

    async Task<IEnumerable<FailedPartition>> ApplyChangesCore(
        Key key,
        IChangeset<AppendedEvent, ExpandoObject> changeset,
        EventSequenceNumber eventSequenceNumber,
        SinkWriteOptions options)
    {
        _bulkCancellation.Token.ThrowIfCancellationRequested();
        options = options with { Partition = PartitionFor(key, changeset) };
        var plan = PlanWrite(key, changeset);

        if (changeset.HasBeenRemoved())
        {
            var removalFilter = options.Mode == SinkWriteMode.GuardedUpsert
                ? Builders<BsonDocument>.Filter.And(plan.Filter, BelowWatermark(eventSequenceNumber))
                : plan.Filter;
            return await ApplyRemoval(key, eventSequenceNumber, removalFilter, options);
        }

        // Existing conditional updates never insert. GuardedUpsert also admits an absent instance;
        // a duplicate primary key from its guarded filter is a watermark no-op, including in ordered bulk writes.
        var guardedPlan = GuardWithWatermark(plan, key, options.Mode, eventSequenceNumber);
        if (guardedPlan is null)
        {
            return [];
        }

        // A dropped join flush reports every partition of its batch, not just this writer's, and the write still runs.
        // Its outcome is claimed only on a returning path: an exception leaves it in the ledger for the next drain or EndBulk.
        var joinBatch = await FlushBeforeJoin(changeset, options);
        IEnumerable<FailedPartition> failures;
        try
        {
            failures = WriteDirectlyInOneBatch(changeset, options)
                ? await writes.InBatch(() => WriteAfterJoin(key, changeset, eventSequenceNumber, guardedPlan, options))
                : await WriteAfterJoin(key, changeset, eventSequenceNumber, guardedPlan, options);
        }
        catch (MongoWriteException exception) when (options.Mode == SinkWriteMode.GuardedUpsert && IsDuplicateId(exception))
        {
            EvictBulkState(key);
            failures = [];
        }
        return joinBatch is null ? failures : TakeBulkFailures([joinBatch]).Concat(failures).ToArray();
    }

    /// <summary>
    /// Checks whether the writes of a changeset bypass the bulk window and so run as one batch.
    /// </summary>
    /// <remarks>
    /// The direct writes of one changeset (joins, child removals, legacy null repairs and the main write) are one fenced batch, so a
    /// revoked owner commits all or none of them. A changeset whose writes are buffered is fenced by the bulk flush instead.
    /// </remarks>
    /// <param name="changeset">The changeset to write.</param>
    /// <param name="options">The <see cref="SinkWriteOptions"/> of the write.</param>
    /// <returns>True if the writes run as one batch.</returns>
    bool WriteDirectlyInOneBatch(IChangeset<AppendedEvent, ExpandoObject> changeset, SinkWriteOptions options) =>
        !CanBuffer(options) || changeset.HasJoined();

    /// <summary>
    /// Checks whether a write may be buffered; inside a fenced batch nothing is, as it would be flushed outside the batch's transaction.
    /// </summary>
    /// <param name="options">The <see cref="SinkWriteOptions"/> of the write.</param>
    /// <returns>True if the write may be buffered.</returns>
    bool CanBuffer(SinkWriteOptions options) => options.CanBuffer(_isBulkMode) && !writes.IsInBatch;

    static Key PartitionFor(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset) =>
        changeset.Incoming is { } incoming ? (Key)incoming.Context.EventSourceId : key;

    async Task<IEnumerable<FailedPartition>> WriteAfterJoin(
        Key key,
        IChangeset<AppendedEvent, ExpandoObject> changeset,
        EventSequenceNumber eventSequenceNumber,
        WritePlan plan,
        SinkWriteOptions options)
    {
        // Converting runs the joins, which are matched by what the documents hold now. A join on a child this changeset
        // removes from all has to find the documents still holding it, so the children are removed only afterwards.
        var converted = await changesetConverter.ToUpdateDefinition(key, changeset, eventSequenceNumber);
        await RemoveChildrenFromAll(changeset);

        // ChangesetConverter has already executed the correctly filtered UpdateMany for the root join.
        // Any remaining direct root PropertiesChanged have no single _id target, so issuing the follow-up
        // UpdateOne would pick an arbitrary document (Filter.Empty) and corrupt it.
        if (!converted.hasChanges || plan.ShouldSuppressRootUpdateAfterRootLevelJoin)
        {
            return [];
        }

        var write = ScopeToExistingArrays(new UpdateWrite(key, eventSequenceNumber, converted, plan.Filter, plan.IsUpsert));
        if (!plan.UsesJoinTargetsOnlyFilter)
        {
            await UnsetNullParents(write, options);
        }

        return await WriteUpdate(write, changeset, options);
    }

    async Task<BulkBatch?> FlushBeforeJoin(IChangeset<AppendedEvent, ExpandoObject> changeset, SinkWriteOptions options)
    {
        // Flush pending operations before a join reads committed data. The batch is taken under the bulk lock.
        if (!_isBulkMode || !changeset.HasJoined())
        {
            return null;
        }

        if (options.DrainedBatches is { } drainedBatches)
        {
            var drain = await DrainBulk();
            drainedBatches.UnionWith(drain.Batches);
            EvictDrainedCache(drain.Batches);
            return null;
        }

        return await CompleteNextBulkBatch();
    }

    WritePlan? GuardWithWatermark(WritePlan plan, Key key, SinkWriteMode mode, EventSequenceNumber eventSequenceNumber)
    {
        if (!IsGuardedWrite(mode, plan, eventSequenceNumber))
        {
            return plan;
        }

        if (IsAlreadyApplied(key, eventSequenceNumber))
        {
            return null;
        }

        return plan with { Filter = Builders<BsonDocument>.Filter.And(plan.Filter, BelowWatermark(eventSequenceNumber)), IsUpsert = mode == SinkWriteMode.GuardedUpsert && plan.IsUpsert };
    }

    static bool IsGuardedWrite(SinkWriteMode mode, WritePlan plan, EventSequenceNumber eventSequenceNumber) =>
        mode is SinkWriteMode.OnlyWhenAdvancingWatermark or SinkWriteMode.GuardedUpsert && !plan.UsesJoinTargetsOnlyFilter && eventSequenceNumber.IsActualValue;

    WritePlan PlanWrite(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset)
    {
        var hasDirectKeyScopedChanges = changeset.Changes.Any(IsDirectKeyScopedChange);
        var hasConstructiveChanges = changeset.Changes.Any(IsConstructiveChange);

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

        // Pulling a child from an absent root must not recreate that root. A changeset that also adds a child
        // can still construct it, but a removal without an addition only updates an existing document.
        var onlyRemovesChildren = changeset.Changes.Any(change => change is ChildRemoved) && !changeset.Changes.Any(change => change is ChildAdded);
        var isUpsert = !onlyRemovesChildren && IsUpsertWrite(onlyPropertyUpdatesAlongsideJoin, isRootLevelJoin, hasKeyedFrom);

        return new(filter, isUpsert, usesJoinTargetsOnlyFilter, shouldSuppressRootUpdateAfterRootLevelJoin);
    }

    static bool IsUpsertWrite(bool onlyPropertyUpdatesAlongsideJoin, bool isRootLevelJoin, bool hasKeyedFrom) =>
        !onlyPropertyUpdatesAlongsideJoin && (!isRootLevelJoin || hasKeyedFrom);

    async Task<IEnumerable<FailedPartition>> ApplyRemoval(Key key, EventSequenceNumber eventSequenceNumber, FilterDefinition<BsonDocument> filter, SinkWriteOptions options)
    {
        if (CanBuffer(options))
        {
            var cacheKey = converter.ToBsonValue(key).ToString()!;
            if (await AddToBulk(new DeleteOneModel<BsonDocument>(filter), key, options.PartitionOr(key), eventSequenceNumber, () =>
            {
                if (options.Mode == SinkWriteMode.GuardedUpsert)
                {
                    InvalidateGuardedBulkState(key);
                }
                else
                {
                    ForgetBulkState(cacheKey);
                }
            }))
            {
                return await FlushBulkIfNeeded();
            }
        }

        await writes.DeleteOne(Collection, filter);
        if (options.IsImmediate)
        {
            writes.AfterCommit(() => EvictBulkState(key));
        }

        return [];
    }

    void EvictBulkState(Key key) => EvictBulkState(converter.ToBsonValue(key).ToString()!);

    void EvictBulkState(string cacheKey)
    {
        _bulkPendingDeletes.TryRemove(cacheKey, out _);
        EvictBulkCache(cacheKey);
    }

    void EvictBulkCache(string cacheKey)
    {
        _bulkStateCache.TryRemove(cacheKey, out _);
        _bulkKeysByCacheKey.TryRemove(cacheKey, out _);
        _bulkWatermarks.TryRemove(cacheKey, out _);
    }

    void EvictDrainedCache(IEnumerable<BulkBatch> batches)
    {
        lock (_bulkLock)
        {
            foreach (var key in batches.SelectMany(batch => batch.Metadata.Values).Select(metadata => metadata.ReadModelKey).Distinct())
            {
                EvictBulkCache(converter.ToBsonValue(key).ToString()!);
            }
        }
    }

    void ForgetBulkState(string cacheKey)
    {
        EvictBulkState(cacheKey);

        // Marked after the operation is queued, never before: a flush that observes the mark without the
        // operation would clear it while the delete is still pending, which is the failure this prevents.
        _bulkPendingDeletes[cacheKey] = ++_pendingDeleteVersion;
    }

    bool IsAlreadyApplied(Key key, EventSequenceNumber eventSequenceNumber) =>
        _isBulkMode &&
        _bulkWatermarks.TryGetValue(converter.ToBsonValue(key).ToString()!, out var applied) &&
        applied >= eventSequenceNumber.Value;

    async Task UnsetNullParents(UpdateWrite write, SinkWriteOptions options)
    {
        var (_, _, converted, filter, _) = write;

        // Reads omit BSON nulls, so a missing parent in the initial state can still be
        // present as null in an older document. Unset only that legacy null (not an
        // existing object) before dotted leaf sets. Keep these ordered with the main
        // write in bulk mode, and preserve the watermark guard on redelivery.
        var parents = converted.NullParentPaths.Select(path => (Path: path, Filters: (IReadOnlyList<BsonDocumentArrayFilterDefinition<BsonDocument>>)[]))
            .Concat(converted.NullArrayParents.Select(_ => (_.Path, Filters: _.ArrayFilters)))
            .OrderBy(_ => _.Path.Count(ch => ch == '.'));

        foreach (var parent in parents)
        {
            var nullFilter = parent.Filters.Count == 0
                ? Builders<BsonDocument>.Filter.And(filter, Builders<BsonDocument>.Filter.Type(parent.Path, BsonType.Null))
                : filter;
            var unset = Builders<BsonDocument>.Update.Unset(parent.Path);
            var operation = new UpdateOneModel<BsonDocument>(nullFilter, unset) { ArrayFilters = parent.Filters };
            if (!await TryBufferOperation(operation, write, options))
            {
                await writes.UpdateOne(Collection, nullFilter, unset, new UpdateOptions { ArrayFilters = parent.Filters });
            }
        }
    }

    async Task<bool> TryBufferOperation(WriteModel<BsonDocument> operation, UpdateWrite write, SinkWriteOptions options)
    {
        if (!CanBuffer(options))
        {
            return false;
        }

        return await AddToBulk(operation, write.Key, options.PartitionOr(write.Key), write.EventSequenceNumber);
    }

    async Task<IEnumerable<FailedPartition>> WriteUpdate(UpdateWrite write, IChangeset<AppendedEvent, ExpandoObject> changeset, SinkWriteOptions options)
    {
        if (await TryBufferUpdate(write, changeset, options) is { } failures)
        {
            return failures;
        }

        await writes.UpdateOne(
            Collection,
            write.Filter,
            write.Converted.UpdateDefinition,
            new UpdateOptions
            {
                IsUpsert = write.IsUpsert,
                ArrayFilters = write.Converted.ArrayFilters
            });
        CacheImmediateWrite(write, changeset, options);

        return [];
    }

    async Task<IEnumerable<FailedPartition>?> TryBufferUpdate(UpdateWrite write, IChangeset<AppendedEvent, ExpandoObject> changeset, SinkWriteOptions options)
    {
        if (!CanBuffer(options))
        {
            return null;
        }

        var updateModel = new UpdateOneModel<BsonDocument>(write.Filter, write.Converted.UpdateDefinition)
        {
            IsUpsert = write.IsUpsert,
            ArrayFilters = write.Converted.ArrayFilters
        };
        var hasJoined = changeset.HasJoined();
        var cacheState = hasJoined || options.Mode == SinkWriteMode.GuardedUpsert ? null : CreateCacheState(write.Key, changeset, write.EventSequenceNumber);

        void WhenQueued()
        {
            // Guarded writes may be rejected without failing the batch. Neither their state nor their
            // watermark is a safe answer for another operation in this bulk window.
            if (options.Mode == SinkWriteMode.GuardedUpsert)
            {
                InvalidateGuardedBulkState(write.Key);
                if (updateModel.IsUpsert)
                {
                    _guardedBulkUpserts.Add(updateModel);
                }
            }
            else
            {
                cacheState?.Invoke();
            }
        }

        if (await AddToBulk(updateModel, write.Key, options.PartitionOr(write.Key), write.EventSequenceNumber, WhenQueued))
        {
            return hasJoined ? await ExecuteBulk() : await FlushBulkIfNeeded();
        }

        return null;
    }

    void CacheImmediateWrite(UpdateWrite write, IChangeset<AppendedEvent, ExpandoObject> changeset, SinkWriteOptions options)
    {
        if (!options.IsImmediate)
        {
            return;
        }

        if (options.Mode == SinkWriteMode.GuardedUpsert)
        {
            writes.AfterCommit(() => EvictBulkState(write.Key));
        }
        else if (_isBulkMode && !changeset.HasJoined())
        {
            writes.AfterCommit(CreateCacheState(write.Key, changeset, write.EventSequenceNumber));
        }
    }

    Action CreateCacheState(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber eventSequenceNumber)
    {
        var cacheKey = converter.ToBsonValue(key).ToString()!;
        return () =>
        {
            _bulkStateCache[cacheKey] = changeset.CurrentState;
            _bulkKeysByCacheKey[cacheKey] = key;
            _bulkPendingDeletes.TryRemove(cacheKey, out _);
            if (eventSequenceNumber.IsActualValue)
            {
                RecordBulkWatermark(cacheKey, eventSequenceNumber.Value);
            }
        };
    }

    /// <inheritdoc/>
    public Task BeginBulk() => BeginBulk(isReplay: false);

    Task BeginBulk(bool isReplay)
    {
        _bulkCancellation.Token.ThrowIfCancellationRequested();
        if (_bulkWindowIsReplay && !isReplay)
        {
            collections.AbandonReplay();
        }

        lock (_bulkLock)
        {
            OpenBulkWindow(isReplay);
        }

        ClearBulkState();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ResumeBulk() => ResumeBulk(isReplay: false);

    Task ResumeBulk(bool isReplay)
    {
        _bulkCancellation.Token.ThrowIfCancellationRequested();
        if (_bulkWindowIsReplay && !isReplay)
        {
            collections.AbandonReplay();
        }

        lock (_bulkLock)
        {
            // Preserve queued writes only when they belong to the collection this mode will flush to.
            var hasOpenWindow = _isBulkMode && _bulkWindowId is not null;
            if (hasOpenWindow && _bulkWindowIsReplay == isReplay)
            {
                return Task.CompletedTask;
            }

            OpenBulkWindow(isReplay);
        }

        ClearBulkState();
        return Task.CompletedTask;
    }

    void OpenBulkWindow(bool isReplay)
    {
        _isBulkMode = true;
        _bulkWindowId = new(Guid.NewGuid());
        _bulkWindowIsReplay = isReplay;
        _bulkWindowHasFailedWrites = false;
        _inFlight.Clear();
        ClearBulkOperations();
    }

    /// <inheritdoc/>
    public async Task<BulkFlushResult> FlushBulk()
    {
        BulkWindowId? windowId;
        lock (_bulkLock)
        {
            windowId = _bulkWindowId;
        }

        var drain = await DrainBulk();
        lock (_bulkLock)
        {
            // Never confirm a window that was replaced or closed while its writes were draining.
            if (windowId != _bulkWindowId)
            {
                throw new BulkWindowLost();
            }

            var failures = TakeBulkFailures(drain.Batches);

            // Claiming an outcome is not proof that its failed partitions have been recorded durably.
            // Keep the window tainted for its lifetime, independently of exclusive failure reporting.
            if (_bulkWindowHasFailedWrites && failures.Count == 0)
            {
                throw new BulkWindowHasFailedWrites();
            }

            return new(windowId, failures);
        }
    }

    void ClearBulkOperations()
    {
        _bulkOperations.Clear();
        _bulkDestinations.Clear();
        _bulkOperationMetadata.Clear();
        _guardedBulkUpserts.Clear();
        _currentBulkSize = 0;
    }

    void ClearBulkState()
    {
        _bulkHasGuardedWrites = false;
        _bulkStateCache.Clear();
        _bulkKeysByCacheKey.Clear();
        _bulkWatermarks.Clear();
        _bulkPendingDeletes.Clear();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Admission to the window closes first, so the final flush holds every write that was accepted into it and the
    /// closing finishes however many writers keep arriving. A write arriving while the window closes waits for it to
    /// finish and is then written directly, after the flushed batch.
    /// </remarks>
    public Task<IEnumerable<FailedPartition>> EndBulk() => EndBulk(isShutdown: false, throwOnFinalFlushError: false, CancellationToken.None);

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> EndBulk(CancellationToken cancellationToken) => EndBulk(isShutdown: true, throwOnFinalFlushError: false, cancellationToken);

    async Task<IEnumerable<FailedPartition>> EndBulk(bool isShutdown, bool throwOnFinalFlushError, CancellationToken cancellationToken)
    {
        FailedPartition[] pending;
        lock (_bulkLock)
        {
            pending = _inFlight.SelectMany(batch => batch.Metadata.Values).Concat(_bulkOperationMetadata.Values)
                .GroupBy(metadata => metadata.EventSourceId)
                .Select(partition => new FailedPartition(partition.Key, partition.MinBy(metadata => metadata.SequenceNumber.Value).SequenceNumber)
                {
                    Reason = "MongoDB bulk write outcome unknown (shutdown deadline)",
                    OutcomeUnknown = true
                }).ToArray();
        }

        // Cancellation is terminal for this sink by design: this path runs while its silo is stopping.
        await using var cancellation = cancellationToken.Register(_bulkCancellation.Cancel);
        TaskCompletionSource closing;
        try
        {
            closing = await BeginClosing(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _bulkCancellation.Cancel();
            return pending;
        }

        // Bulk mode ends whatever the final flush does: a sink left in bulk mode would hold every later
        // write back until a thousand of them had queued up.
        try
        {
            var drain = await DrainBulk(cancellationToken);

            // A final flush whose outcome is unknown reports every partition it held. Only a replay throws instead,
            // because it abandons the rebuilt collection those partitions would have been recorded against.
            if (throwOnFinalFlushError && drain.Queued is { } queued && TakeBatchError(queued) is { } error)
            {
                ExceptionDispatchInfo.Throw(error);
            }

            return TakeBulkFailures(drain.Batches, logErrors: isShutdown);
        }
        finally
        {
            if (cancellationToken.IsCancellationRequested)
            {
                // WaitAsync can finish before the deadline's cancellation callback runs. Disposing the
                // registration would then remove that callback, so cancel the driver explicitly too.
                _bulkCancellation.Cancel();
            }

            lock (_bulkLock)
            {
                _isBulkMode = false;
                _bulkWindowId = null;
                _bulkWindowIsReplay = false;
                _bulkWindowHasFailedWrites = false;
                _inFlight.Clear();
                ClearBulkOperations();
            }

            ClearBulkState();

            lock (_bulkLock)
            {
                _closing = null;
            }

            closing.SetResult();
        }
    }

    async Task<TaskCompletionSource> BeginClosing(CancellationToken cancellationToken)
    {
        var closing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        while (true)
        {
            Task previousClosing;
            lock (_bulkLock)
            {
                if (_closing is null)
                {
                    _closing = closing;
                    return closing;
                }

                previousClosing = _closing.Task;
            }

            await previousClosing.WaitAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public Task PrepareInitialRun()
    {
        RefuseGenerationLifecycle();
        return collections.PrepareInitialRun();
    }

    /// <inheritdoc/>
    public async Task BeginReplay(ReplayContext context)
    {
        RefuseGenerationLifecycle();
        await collections.BeginReplay(context);

        // A replay writes into its own shadow collection and that collection is renamed into place at the end,
        // taking its own indexes with it - and only its own. Indexes were ensured once when the sink was built,
        // against the collection that the swap replaces, so without this the promoted collection comes up with
        // none of them until something rebuilds the sink. Recreating them is exactly what the declaration on the
        // read model is for (#3942).
        await EnsureIndexes();
        await BeginBulk(isReplay: true);
    }

    /// <inheritdoc/>
    public async Task ResumeReplay(ReplayContext context)
    {
        RefuseGenerationLifecycle();
        await collections.ResumeReplay(context);
        await EnsureIndexes();
        await ResumeBulk(isReplay: true);
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
        RefuseGenerationLifecycle();
        FailedPartition[] failedPartitions;
        try
        {
            failedPartitions = (await EndBulk(isShutdown: false, throwOnFinalFlushError: true, CancellationToken.None)).ToArray();
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

        _primaryState = null;
        try
        {
            await collections.EndReplay(context);
        }
        finally
        {
            _primaryState = null;
        }

        return failedPartitions;
    }

    /// <inheritdoc/>
    public async Task LeaveReplay()
    {
        RefuseGenerationLifecycle();

        // Whatever is still held back belongs to a replay that has been promoted without it. Writing it to the replay
        // collection would lose it, so it goes to the read model's own collection once replay mode is left.
        collections.AbandonReplay();
        var failedPartitions = (await EndBulk()).ToArray();
        if (failedPartitions.Length > 0)
        {
            logger.LeavingReplayWithFailedPartitions(readModel.Identifier, failedPartitions.Length, failedPartitions.Count(partition => partition.OutcomeUnknown));
        }
    }

    /// <inheritdoc/>
    public Task Remove(ReadModelContainerName containerName)
    {
        RefuseGenerationLifecycle();
        return collections.Remove(containerName);
    }

    /// <inheritdoc/>
    public async Task<Option<Key>> TryFindRootKeyByChildValue(PropertyPath childPropertyPath, object childValue)
    {
        await SettleGuardedBulkReads();
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
        if (GenerationIndexDdl is not null)
        {
            await GenerationIndexDdl(EnsureGenerationIndexes);
            return;
        }
        var collection = Collection;
        using var cursor = await collection.Indexes.ListAsync();
        var existingIndexes = await cursor.ToListAsync();
        var collation = new Lazy<Task<BsonValue>>(() => GetCollectionCollation(collection));

        foreach (var indexDefinition in readModel.Indexes)
        {
            ReadModelIndexKeys.Validate(indexDefinition, readModel.GetSchemaForLatestGeneration());
            var indexName = ReadModelIndexKeys.Name(indexDefinition);
            var keys = ReadModelIndexKeys.For(indexDefinition);

            var candidates = existingIndexes.FindAll(index => HasMatchingKeyAndOptions(index, keys));
            if (await IsIndexPresent(candidates, indexName, collation))
            {
                continue;
            }

            var indexModel = new CreateIndexModel<BsonDocument>(
                keys,
                new CreateIndexOptions { Name = indexName, Background = true });

            await collection.Indexes.CreateOneAsync(indexModel);
        }
    }

    async Task EnsureGenerationIndexes()
    {
        var collection = Collection;
        List<BsonDocument> existing;
        try
        {
            using var cursor = await collection.Indexes.ListAsync();
            existing = await cursor.ToListAsync();
        }
        catch (MongoCommandException error) when (error.Code == 26)
        {
            existing = [];
        }
        var collation = new Lazy<Task<BsonValue>>(() => GetCollectionCollation(collection));
        foreach (var index in readModel.Indexes)
            await EnsureGenerationIndex(collection, existing, index, collation);
    }

    async Task EnsureGenerationIndex(IMongoCollection<BsonDocument> collection, List<BsonDocument> existing, IndexDefinition index, Lazy<Task<BsonValue>> collation)
    {
        ReadModelIndexKeys.Validate(index, readModel.GetSchemaForLatestGeneration());
        var name = ReadModelIndexKeys.Name(index);
        var keys = ReadModelIndexKeys.For(index);
        var previous = existing.FirstOrDefault(document => document["name"].AsString == name);
        if (previous is not null)
        {
            if (!HasMatchingKeyAndOptions(previous, keys)) throw new GenerationIndexConflict(name);
            if (previous.GetValue("hidden", false).ToBoolean()) throw new GenerationIndexConflict(name);
            if (!GetCollation(previous).Equals(await collation.Value)) throw new GenerationIndexConflict(name);
            return;
        }
        try
        {
            await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(keys, new CreateIndexOptions { Name = name, Background = true }));
        }
        catch (MongoCommandException error) when (error.Code is 85 or 86)
        {
            throw new GenerationIndexConflict(name, error);
        }
    }

    void RefuseGenerationLifecycle()
    {
        if (GenerationIndexDdl is not null) throw new Cratis.Chronicle.Storage.Generations.Bindings.MaterializationBindingsNotSupported();
    }

    static async Task<bool> IsIndexPresent(List<BsonDocument> candidates, string indexName, Lazy<Task<BsonValue>> collation)
    {
        if (candidates.Exists(index => index.GetValue("name", BsonNull.Value) == indexName))
        {
            return true;
        }

        if (candidates.Count == 0)
        {
            return false;
        }

        var effectiveCollation = await collation.Value;
        return candidates.Exists(index => GetCollation(index).Equals(effectiveCollation));
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
                cancellationToken => ReadObservedPage(new(collection, containerName, isPrimary, skip, take), cancellationToken))
            .Where(page => page is not null)
            .Select(page => page!);
    }

    async Task<IEnumerable<ExpandoObject>?> ReadObservedPage(PageRequest request, CancellationToken cancellationToken)
    {
        var (collection, containerName, isPrimary, skip, take) = request;
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

    /// <summary>
    /// Narrows a write positioned in an array by array filters to the documents holding that array. Against a document
    /// without it — a root whose children were all removed, or one the write would upsert — MongoDB fails the whole update
    /// with error 2 and the partition fails on every retry. Such a write is a no-op instead, exactly as when the array
    /// exists and no element matches.
    /// </summary>
    /// <param name="write">The write to narrow.</param>
    /// <returns>The narrowed write.</returns>
    static UpdateWrite ScopeToExistingArrays(UpdateWrite write)
    {
        var requiredArrayPaths = write.Converted.RequiredArrayPaths;
        if (requiredArrayPaths.Count == 0)
        {
            return write;
        }

        var filter = Builders<BsonDocument>.Filter.And([write.Filter, .. requiredArrayPaths.Select(path => Builders<BsonDocument>.Filter.Exists(path))]);
        return write with { Filter = filter, IsUpsert = false };
    }

    static bool IsDirectKeyScopedChange(Change change) =>
        change is PropertiesChanged<ExpandoObject> or ChildAdded or ChildRemoved;

    static bool IsConstructiveChange(Change change) =>
        change is ChildAdded or ChildRemoved;

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

    static bool HasMatchingKeyAndOptions(BsonDocument index, BsonDocument keys) =>
        index.GetValue("key", BsonNull.Value).Equals(keys) &&
        index.GetValue("unique", false) == false &&
        index.GetValue("prepareUnique", false) == false &&
        index.GetValue("sparse", false) == false &&
        index.GetValue("hidden", false) == false &&
        !index.Contains("partialFilterExpression") &&
        !index.Contains("expireAfterSeconds");

    static BsonValue GetCollation(BsonDocument definition)
    {
        // MongoDB 9 lists simple index collation explicitly; older servers omit it. Both mean binary comparison.
        return definition.GetValue("collation", new BsonDocument("locale", "simple"));
    }

    async Task<BsonValue> GetCollectionCollation(IMongoCollection<BsonDocument> collection)
    {
        // An index without an explicit collation inherits the collection's default. Compare that effective
        // collation as well as the key, so an index for different string comparisons is not mistaken for ours.
        using var cursor = await collection.Database.ListCollectionsAsync(new ListCollectionsOptions
        {
            Filter = new BsonDocument("name", collection.CollectionNamespace.CollectionName)
        });
        var definitions = await cursor.ToListAsync();
        var options = definitions.FirstOrDefault()?.GetValue("options", new BsonDocument()).AsBsonDocument ?? new BsonDocument();
        return GetCollation(options);
    }

    /// <summary>
    /// Queues an operation into the open bulk window, together with the bulk state that describes it.
    /// </summary>
    /// <param name="operation">The <see cref="WriteModel{TDocument}"/> to queue.</param>
    /// <param name="key">The <see cref="Key"/> the operation is for.</param>
    /// <param name="partition">The event source id of the partition the operation is reported under if its batch fails.</param>
    /// <param name="eventSequenceNumber">The <see cref="EventSequenceNumber"/> the operation applies.</param>
    /// <param name="whenQueued">Optional update of the bulk state, made atomically with queueing the operation.</param>
    /// <returns>True if the operation was queued; false if no bulk window is open and the caller must write directly.</returns>
    /// <remarks>
    /// Checking the window and queueing happen under one lock, so <see cref="EndBulk()"/> either flushes the operation or
    /// never admits it. While a window is closing, the caller waits for it to finish before it is told to write directly.
    /// </remarks>
    async Task<bool> AddToBulk(WriteModel<BsonDocument> operation, Key key, Key partition, EventSequenceNumber eventSequenceNumber, Action? whenQueued = null)
    {
        while (true)
        {
            Task? closing;
            BulkBatch? full = null;
            lock (_bulkLock)
            {
                if (!_isBulkMode)
                {
                    return false;
                }

                closing = _closing?.Task;
                if (closing is null)
                {
                    var destination = writes.MaxBatchSize.HasValue ? converter.ToBsonValue(key) : null;
                    if (writes.MaxBatchSize is { } cap && _bulkDestinations.Count >= cap && !_bulkDestinations.Contains(destination!))
                    {
                        // Detach under the admission lock: concurrent producers cannot overfill a fenced batch.
                        full = TakeBulkBatch();
                    }
                    else
                    {
                        var operationIndex = _bulkOperations.Count;
                        _bulkOperations.Add(operation);
                        if (destination is not null)
                        {
                            _bulkDestinations.Add(destination);
                        }
                        _bulkOperationMetadata[operationIndex] = (key, partition, eventSequenceNumber);
                        _currentBulkSize += EstimateOperationSize(operation);
                        whenQueued?.Invoke();
                        return true;
                    }
                }
            }

            if (full is not null)
            {
                // Leave failures in the outcome ledger for FlushBulk/EndBulk; never consume and discard them here.
                await CompleteBulkBatch(full);
            }
            else
            {
                await closing!;
            }
        }
    }

    async Task<IEnumerable<FailedPartition>> FlushBulkIfNeeded()
    {
        bool shouldFlush;
        lock (_bulkLock)
        {
            shouldFlush = _bulkOperations.Count >= MaxBulkOperations || _currentBulkSize >= MaxBulkSizeInBytes ||
                (writes.MaxBatchSize is { } cap && _bulkDestinations.Count >= cap);
        }

        if (shouldFlush)
        {
            return await ExecuteBulk();
        }

        return [];
    }

    async Task<IEnumerable<FailedPartition>> ExecuteBulk()
    {
        // A batch whose outcome is unknown reports every partition it held, whichever writer queued them,
        // instead of throwing to the one writer that happened to fill it. It is never sent again.
        var batch = await CompleteNextBulkBatch();
        return batch is null ? [] : TakeBulkFailures([batch]);
    }

    async Task<BulkBatch?> CompleteNextBulkBatch()
    {
        var batch = TakeBulkBatch();
        if (batch is not null)
        {
            await CompleteBulkBatch(batch);
        }

        return batch;
    }

    async Task<BulkDrain> DrainBulk(CancellationToken cancellationToken = default)
    {
        BulkBatch? queued;
        BulkBatch[] pending;
        lock (_bulkLock)
        {
            queued = TakeBulkBatch();
            pending = _unreportedBulkOutcomes.Keys.Concat(_inFlight).Distinct().ToArray();
        }

        if (queued is not null)
        {
            // CompleteBulkBatch observes every driver failure, even if the driver ignores cancellation.
            _ = CompleteBulkBatch(queued);
        }

        try
        {
            // Completions never fault: failures belong to the outcome ledger.
            await Task.WhenAll(pending.Select(batch => batch.Completion.Task)).WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            foreach (var batch in pending.Where(batch => !batch.Completion.Task.IsCompleted))
            {
                RecordBulkOutcome(batch, UnknownBatchOutcome(batch, exception));
            }
        }
        return new(queued, pending);
    }

    async Task CompleteBulkBatch(BulkBatch batch)
    {
        BulkOutcome outcome;
        try
        {
            await Task.WhenAll(batch.Predecessors).WaitAsync(_bulkCancellation.Token);
            outcome = new((await WriteBatch(batch, _bulkCancellation.Token)).ToArray(), null);
        }
        catch (Exception exception)
        {
            outcome = UnknownBatchOutcome(batch, exception);
        }

        try
        {
            RecordBulkOutcome(batch, outcome);
            foreach (var pendingDelete in batch.FlushedPendingDeletes)
            {
                // A newer mark belongs to a delete queued after this batch was detached.
                ((ICollection<KeyValuePair<string, long>>)_bulkPendingDeletes).Remove(pendingDelete);
            }
        }
        finally
        {
            // The batch runs detached, so whoever awaits its completion must never be left waiting.
            batch.Completion.TrySetResult();
        }
    }

    void RecordBulkOutcome(BulkBatch batch, BulkOutcome outcome)
    {
        lock (_bulkLock)
        {
            if (batch.HasOutcome)
            {
                return;
            }

            batch.HasOutcome = true;
            _unreportedBulkOutcomes.Add(batch, outcome);
            _bulkWindowHasFailedWrites |= outcome.Failures.Length > 0 || outcome.Error is not null;
            _inFlight.Remove(batch);
            var failedPartitions = outcome.Failures.Select(failure => failure.EventSourceId).ToHashSet();
            var failedReadModels = batch.Metadata.Values
                .Where(metadata => failedPartitions.Contains(metadata.EventSourceId))
                .Select(metadata => metadata.ReadModelKey)
                .Distinct();
            foreach (var readModelKey in failedReadModels)
            {
                EvictBulkCache(converter.ToBsonValue(readModelKey).ToString()!);
            }
        }
    }

    static BulkOutcome UnknownBatchOutcome(BulkBatch batch, Exception error)
    {
        var failures = batch.Metadata.Values.GroupBy(metadata => metadata.EventSourceId)
            .Select(partition => new FailedPartition(partition.Key, partition.MinBy(metadata => metadata.SequenceNumber.Value).SequenceNumber)
            {
                Reason = $"MongoDB bulk write outcome unknown ({error.GetType().Name})",
                OutcomeUnknown = true
            }).ToArray();
        return new(failures, error);
    }

    Exception? TakeBatchError(BulkBatch batch)
    {
        lock (_bulkLock)
        {
            if (!_unreportedBulkOutcomes.TryGetValue(batch, out var outcome))
            {
                return null;
            }

            if (outcome.Error is null)
            {
                return null;
            }

            _unreportedBulkOutcomes.Remove(batch);
            return outcome.Error;
        }
    }

    List<FailedPartition> TakeBulkFailures(IEnumerable<BulkBatch> batches, bool logErrors = false)
    {
        var failures = new List<FailedPartition>();
        lock (_bulkLock)
        {
            foreach (var batch in batches)
            {
                if (_unreportedBulkOutcomes.Remove(batch, out var outcome))
                {
                    // An unclaimed exception becomes failures for its affected keys. Its owner may
                    // instead claim and throw it, but never both: each entry is handed out once.
                    if (logErrors && outcome.Error is not null)
                    {
                        logger.ShutdownBulkWriteOutcomeUnknown(Collection.CollectionNamespace.CollectionName, readModel.Identifier, outcome.Error.GetType().Name, outcome.Failures.Length);
                    }

                    failures.AddRange(outcome.Failures);
                }
            }
        }

        return failures;
    }

    BulkBatch? TakeBulkBatch()
    {
        lock (_bulkLock)
        {
            if (_bulkOperations.Count == 0)
            {
                return null;
            }

            // Only the marks that exist now can belong to operations in this snapshot; a mark added afterwards
            // belongs to a delete still queued and must survive the flush.
            var batch = new BulkBatch([.._bulkOperations], new(_bulkOperationMetadata), [.._bulkPendingDeletes], _inFlight.Select(batch => batch.Completion.Task).ToArray(), new HashSet<WriteModel<BsonDocument>>(_guardedBulkUpserts));
            ClearBulkOperations();
            _inFlight.Add(batch);
            return batch;
        }
    }

    async Task<IEnumerable<FailedPartition>> WriteBatch(BulkBatch batch, CancellationToken cancellationToken)
    {
        var remainingIndexes = Enumerable.Range(0, batch.Operations.Count).ToList();
        while (remainingIndexes.Count > 0)
        {
            var remaining = remainingIndexes.ConvertAll(index => batch.Operations[index]);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await writes.BulkWrite(Collection, remaining, batch.GuardedUpserts, cancellationToken).WaitAsync(cancellationToken);
                break;
            }
            catch (MongoBulkWriteException<BsonDocument> ex)
            {
                var resumeFrom = ResumeAfterFailure(batch, ex, remaining, remainingIndexes);
                if (resumeFrom is null)
                {
                    break;
                }

                remainingIndexes = resumeFrom;
            }
            catch (MongoBulkWriteException ex)
            {
                var reasons = ReportBulkWriteErrors(ex, remainingIndexes, batch.Metadata);
                AddFailedPartitions(remainingIndexes, batch.Metadata, batch.FailedPartitions, reasons);
                break;
            }
        }

        return batch.FailedPartitions
            .Select(partition => new FailedPartition(partition.Key, partition.Value.SequenceNumber) { Reason = partition.Value.Reason })
            .ToArray();
    }

    /// <summary>
    /// Records the failure of an ordered bulk write and works out which operations can still be written.
    /// </summary>
    /// <returns>The indexes still to write, or null when the write cannot be resumed.</returns>
    List<int>? ResumeAfterFailure(
        BulkBatch batch,
        MongoBulkWriteException<BsonDocument> exception,
        List<WriteModel<BsonDocument>> remaining,
        List<int> remainingIndexes)
    {
        // The write seam may already have committed a prefix and rejected a guarded upsert before
        // a later request failed. Driver error indexes then belong to that resubmitted suffix.
        if (batch.GuardedUpserts.Count > 0 && exception.Result?.ProcessedRequests.Count > 0)
        {
            var skipped = remaining.IndexOf(exception.Result.ProcessedRequests[0]);
            if (skipped > 0)
            {
                remaining = remaining.Skip(skipped).ToList();
                remainingIndexes = remainingIndexes.Skip(skipped).ToList();
            }
        }
        var reasons = ReportBulkWriteErrors(exception, remainingIndexes, batch.Metadata);

        if (!CanResumeAfter(exception, remaining))
        {
            AddFailedPartitions(remainingIndexes, batch.Metadata, batch.FailedPartitions, reasons);
            return null;
        }

        var failedOffset = exception.WriteErrors[0].Index;
        AddFailedPartitions([remainingIndexes[failedOffset]], batch.Metadata, batch.FailedPartitions, reasons);

        // The observer will replay a failed partition from its earliest failed sequence number.
        // Do not write any later changes for that partition ahead of the replayed change.
        return remainingIndexes.Skip(failedOffset + 1)
            .Where(index => !batch.Metadata.TryGetValue(index, out var metadata) ||
                !batch.FailedPartitions.ContainsKey(metadata.EventSourceId))
            .ToList();
    }

    static bool CanResumeAfter(MongoBulkWriteException<BsonDocument> exception, List<WriteModel<BsonDocument>> remaining)
    {
        // ProcessedRequests includes the failed request, not just the successful writes.
        // An ordered write can resume only when it identifies an exact processed prefix and
        // an unprocessed suffix. A write concern error leaves the outcome uncertain.
        if (exception.WriteConcernError is not null || exception.WriteErrors.Count != 1)
        {
            return false;
        }

        if (exception.Result?.IsAcknowledged != true)
        {
            return false;
        }

        var failedIndex = exception.WriteErrors[0].Index;
        if (failedIndex < 0 || failedIndex >= remaining.Count)
        {
            return false;
        }

        var processed = exception.Result.ProcessedRequests;
        if (processed.Count != failedIndex + 1)
        {
            return false;
        }

        return processed.SequenceEqual(remaining.Take(processed.Count)) &&
            exception.UnprocessedRequests.SequenceEqual(remaining.Skip(processed.Count));
    }

    static void AddFailedPartitions(
        IEnumerable<int> indexes,
        Dictionary<int, (Key ReadModelKey, Key EventSourceId, EventSequenceNumber SequenceNumber)> metadata,
        Dictionary<Key, (EventSequenceNumber SequenceNumber, string Reason)> failedPartitions,
        BulkWriteFailureReasons reasons)
    {
        foreach (var index in indexes)
        {
            if (metadata.TryGetValue(index, out var operationMetadata) && IsEarliestFailure(operationMetadata, failedPartitions))
            {
                failedPartitions[operationMetadata.EventSourceId] = (operationMetadata.SequenceNumber, reasons.For(index));
            }
        }
    }

    static bool IsEarliestFailure(
        (Key ReadModelKey, Key EventSourceId, EventSequenceNumber SequenceNumber) operationMetadata,
        Dictionary<Key, (EventSequenceNumber SequenceNumber, string Reason)> failedPartitions) =>
        !failedPartitions.TryGetValue(operationMetadata.EventSourceId, out var earliest) ||
        operationMetadata.SequenceNumber.Value < earliest.SequenceNumber.Value;

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
        Dictionary<int, (Key ReadModelKey, Key EventSourceId, EventSequenceNumber SequenceNumber)> metadata)
    {
        var collection = collections.GetCollection().CollectionNamespace?.CollectionName ?? readModel.ContainerName.Value;
        var byOperation = new Dictionary<int, string>();
        var first = string.Empty;

        foreach (var error in exception.WriteErrors)
        {
            var (operationIndex, reason) = ReportWriteError(collection, error, remainingIndexes, metadata);
            byOperation.TryAdd(operationIndex, reason);
            if (first.Length == 0)
            {
                first = reason;
            }
        }

        var concernReason = ReportWriteConcernError(collection, exception, remainingIndexes.Count);
        if (first.Length == 0 && concernReason is not null)
        {
            first = concernReason;
        }

        return new(byOperation, first);
    }

    (int OperationIndex, string Reason) ReportWriteError(
        string collection,
        BulkWriteError error,
        List<int> remainingIndexes,
        Dictionary<int, (Key ReadModelKey, Key EventSourceId, EventSequenceNumber SequenceNumber)> metadata)
    {
        var operationIndex = error.Index >= 0 && error.Index < remainingIndexes.Count ? remainingIndexes[error.Index] : -1;
        var partition = metadata.TryGetValue(operationIndex, out var operation) ? operation.EventSourceId.Value.ToString() ?? string.Empty : string.Empty;
        logger.BulkWriteErrorOccurred(collection, readModel.Identifier, partition, operationIndex, error.Code, error.Category, error.Message);

        return (operationIndex, $"MongoDB write error {error.Code} ({error.Category}): {error.Message}");
    }

    string? ReportWriteConcernError(string collection, MongoBulkWriteException exception, int remainingCount)
    {
        if (exception.WriteConcernError is not { } writeConcernError)
        {
            return null;
        }

        logger.BulkWriteConcernErrorOccurred(collection, readModel.Identifier, writeConcernError.Code, writeConcernError.CodeName, writeConcernError.Message, remainingCount);
        return $"MongoDB write concern error {writeConcernError.Code} ({writeConcernError.CodeName}): {writeConcernError.Message}";
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

        return segmentIndex == pathSegments.Length - 1
            ? ValuesAreEqual(value, targetValue)
            : TryFindValueInChildren(value, pathSegments, segmentIndex, targetValue);
    }

    bool TryFindValueInChildren(object value, IPropertyPathSegment[] pathSegments, int segmentIndex, object targetValue) =>
        value switch
        {
            IEnumerable<object> collection => collection.OfType<ExpandoObject>().Any(item => TryFindValueInDocument(item, pathSegments, segmentIndex + 1, targetValue)),
            ExpandoObject nestedExpando => TryFindValueInDocument(nestedExpando, pathSegments, segmentIndex + 1, targetValue),
            _ => false
        };

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

    async Task RemoveChildrenFromAll(IChangeset<AppendedEvent, ExpandoObject> changeset)
    {
        foreach (var childRemoved in changeset.Changes.OfType<ChildRemovedFromAll>())
        {
            await RemoveChildFromAll(childRemoved);
        }
    }

    async Task RemoveChildFromAll(ChildRemovedFromAll childRemoved)
    {
        var (path, arrayFilters, filter) = BuildChildPath(childRemoved.ChildrenProperty.Segments.ToArray());

        var identifiedByProperty = childRemoved.IdentifiedByProperty.ToMongoDB();
        var childFilter = Builders<BsonDocument>.Filter.Eq(identifiedByProperty, childRemoved.Key.ToBsonValue());
        var update = Builders<BsonDocument>.Update.PullFilter(string.Join('.', path), childFilter);
        await writes.UpdateMany(Collection, filter, update, new UpdateOptions { ArrayFilters = arrayFilters.Count == 0 ? null : arrayFilters });
    }

    static (List<string> Path, List<BsonDocumentArrayFilterDefinition<BsonDocument>> ArrayFilters, FilterDefinition<BsonDocument> Filter) BuildChildPath(IPropertyPathSegment[] segments)
    {
        var names = segments.Select(segment => segment.Value.ToMongoDBPropertyName()).ToArray();
        var path = new List<string>();
        var arrayFilters = new List<BsonDocumentArrayFilterDefinition<BsonDocument>>();
        var filter = Builders<BsonDocument>.Filter.Empty;

        for (var index = 0; index < segments.Length; index++)
        {
            path.Add(names[index]);
            if (segments[index] is not ArrayProperty || index == segments.Length - 1)
            {
                continue;
            }

            if (arrayFilters.Count == 0)
            {
                filter = Builders<BsonDocument>.Filter.Type(string.Join('.', names.Take(index + 1)), BsonType.Array);
            }

            // Each parent element must contain the next array. Missing/null branches are skipped rather
            // than making MongoDB reject the positional update for the entire document.
            var nextArray = FindNextArray(segments, index);
            var identifier = $"a{index}";
            var nextArrayPath = string.Join('.', names.Skip(index + 1).Take(nextArray - index));
            arrayFilters.Add(new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument($"{identifier}.{nextArrayPath}", new BsonDocument("$type", "array"))));
            path.Add($"$[{identifier}]");
        }

        if (arrayFilters.Count == 0)
        {
            filter = Builders<BsonDocument>.Filter.Type(string.Join('.', path), BsonType.Array);
        }

        return (path, arrayFilters, filter);
    }

    static int FindNextArray(IPropertyPathSegment[] segments, int index)
    {
        var nextArray = index + 1;
        while (nextArray < segments.Length - 1 && segments[nextArray] is not ArrayProperty)
        {
            nextArray++;
        }

        return nextArray;
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

    sealed record SinkWriteOptions(SinkWriteMode Mode, HashSet<BulkBatch>? DrainedBatches = null, Key? Partition = null)
    {
        public bool IsImmediate => DrainedBatches is not null;

        public Key PartitionOr(Key key) => Partition ?? key;

        public bool CanBuffer(bool isBulkMode) => isBulkMode && !IsImmediate;
    }

    sealed record WritePlan(
        FilterDefinition<BsonDocument> Filter,
        bool IsUpsert,
        bool UsesJoinTargetsOnlyFilter,
        bool ShouldSuppressRootUpdateAfterRootLevelJoin);

    sealed record UpdateWrite(
        Key Key,
        EventSequenceNumber EventSequenceNumber,
        UpdateDefinitionAndArrayFilters Converted,
        FilterDefinition<BsonDocument> Filter,
        bool IsUpsert);

    sealed record PageRequest(IMongoCollection<BsonDocument> Collection, string ContainerName, bool IsPrimary, int Skip, int Take);

    sealed record BulkBatch(
        List<WriteModel<BsonDocument>> Operations,
        Dictionary<int, (Key ReadModelKey, Key EventSourceId, EventSequenceNumber SequenceNumber)> Metadata,
        KeyValuePair<string, long>[] FlushedPendingDeletes,
        Task[] Predecessors,
        IReadOnlySet<WriteModel<BsonDocument>> GuardedUpserts)
    {
        public Dictionary<Key, (EventSequenceNumber SequenceNumber, string Reason)> FailedPartitions { get; } = [];
        public TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool HasOutcome { get; set; }
    }

    sealed record BulkOutcome(FailedPartition[] Failures, Exception? Error);

    sealed record BulkDrain(BulkBatch? Queued, BulkBatch[] Batches);

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
