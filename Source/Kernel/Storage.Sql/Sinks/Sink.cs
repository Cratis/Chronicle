// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Monads;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static Cratis.Chronicle.Storage.Sql.Sinks.JsonColumnDocuments;

#pragma warning disable SA1202 // Private helpers grouped next to the public methods they back.
#pragma warning disable SA1204 // Static helpers grouped at the bottom for readability.

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// SQL implementation of <see cref="ISink"/> backed by a per-read-model table whose column shape is
/// derived from the read model's <see cref="JsonSchema"/>: each leaf
/// property becomes a real typed column, collections and nested objects become a single JSON column
/// (<c language="csharp">jsonb</c> on PostgreSQL, <c language="csharp">nvarchar(max)</c> on SQL Server, <c language="csharp">TEXT</c> on SQLite). Changes
/// from the projection engine are translated into per-column updates on a tracked
/// <see cref="DynamicReadModelEntity"/>; EF's change tracker turns those into <c language="csharp">UPDATE</c>s that
/// only touch the modified columns — matching MongoDB's <c language="csharp">$set</c> semantics and removing the
/// stale-snapshot overwrite class of bug that whole-document upserts suffered from.
/// </summary>
public partial class Sink : ISink
{
    static readonly IEnumerable<FailedPartition> _noFailedPartitions = [];

    /// <summary>
    /// The exception type suffixes and message fragments identifying a unique constraint violation.
    /// SqliteException error code 19 is SQLITE_CONSTRAINT; the message names UNIQUE / PRIMARY KEY.
    /// PostgreSQL's PostgresException sets SqlState 23505; SQL Server's SqlException sets Number 2627/2601.
    /// We treat them all alike: the row already exists, the caller should retry as UPDATE.
    /// </summary>
    static readonly (string ExceptionSuffix, string[] MessageFragments)[] _uniqueViolations =
    [
        ("SqliteException", ["UNIQUE"]),
        ("PostgresException", ["duplicate key"]),
        ("SqlException", ["PRIMARY KEY", "UNIQUE KEY"])
    ];

    readonly Concepts.EventStoreName _eventStoreName;
    readonly Concepts.EventStoreNamespaceName _namespace;
    readonly IDatabase _database;
    readonly IExpandoObjectConverter _expandoObjectConverter;
    readonly JsonSchema _schema;
    readonly ReadModelIdentifier _readModelIdentifier;
    readonly string _tableName;
    readonly IReadOnlyList<ProjectedColumn> _columns;
    readonly ReplayingTables _replayingTables;
    readonly ILogger<Sink> _logger;
    readonly ReadModelDefinition _readModel;
    readonly ICompositeKeyReplayRecommender? _compositeKeyReplayRecommender;

    /// <summary>
    /// Initializes a new instance of the <see cref="Sink"/> class.
    /// </summary>
    /// <param name="eventStoreName">The <see cref="Concepts.EventStoreName"/> the sink is for.</param>
    /// <param name="namespace">The <see cref="Concepts.EventStoreNamespaceName"/> the sink is for.</param>
    /// <param name="readModel">The <see cref="ReadModelDefinition"/> the sink is for.</param>
    /// <param name="database">The <see cref="IDatabase"/> for accessing SQL storage.</param>
    /// <param name="expandoObjectConverter">The schema-aware <see cref="IExpandoObjectConverter"/>.</param>
    /// <param name="replayingTables">The <see cref="ReplayingTables"/> shared by every sink, telling which tables a replay is rebuilding.</param>
    public Sink(
        Concepts.EventStoreName eventStoreName,
        Concepts.EventStoreNamespaceName @namespace,
        ReadModelDefinition readModel,
        IDatabase database,
        IExpandoObjectConverter expandoObjectConverter,
        ReplayingTables replayingTables)
        : this(eventStoreName, @namespace, readModel, database, expandoObjectConverter, replayingTables, NullLogger<Sink>.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Sink"/> class with replay recovery diagnostics.
    /// </summary>
    /// <param name="eventStoreName">The event store the sink is for.</param>
    /// <param name="namespace">The namespace the sink is for.</param>
    /// <param name="readModel">The read model definition.</param>
    /// <param name="database">The database for accessing SQL storage.</param>
    /// <param name="expandoObjectConverter">The schema-aware converter.</param>
    /// <param name="replayingTables">The shared replay routing state.</param>
    /// <param name="logger">The logger for replay recovery diagnostics.</param>
    /// <param name="compositeKeyReplayRecommender">Optional <see cref="ICompositeKeyReplayRecommender"/> raising a replay recommendation for read models holding legacy composite keys.</param>
    public Sink(
        Concepts.EventStoreName eventStoreName,
        Concepts.EventStoreNamespaceName @namespace,
        ReadModelDefinition readModel,
        IDatabase database,
        IExpandoObjectConverter expandoObjectConverter,
        ReplayingTables replayingTables,
        ILogger<Sink> logger,
        ICompositeKeyReplayRecommender? compositeKeyReplayRecommender = null)
    {
        _logger = logger;
        _readModel = readModel;
        _compositeKeyReplayRecommender = compositeKeyReplayRecommender;
        _replayingTables = replayingTables;
        _eventStoreName = eventStoreName;
        _namespace = @namespace;
        _database = database;
        _expandoObjectConverter = expandoObjectConverter;
        _schema = readModel.GetSchemaForLatestGeneration();
        _readModelIdentifier = readModel.Identifier;
        _tableName = readModel.ContainerName.Value;
        _columns = ProjectedColumns.ForSchema(_schema);
    }

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.SQL;

    /// <summary>
    /// Gets the table name the projection pipeline currently reads and writes through the sink. Resolves to
    /// <c language="csharp">replay-{tableName}</c> while a replay is in progress, and to the primary table name
    /// otherwise. EndReplay swaps the two so the running system observes the rebuilt state atomically. Whether a
    /// replay is in progress is shared by every sink for the table, whichever of them began or ends it.
    /// </summary>
    /// <remarks>
    /// Only the pipeline's own read-modify-write cycle follows the replay: <see cref="ApplyChanges(Key, IChangeset{AppendedEvent, ExpandoObject}, EventSequenceNumber, SinkWriteMode)"/>,
    /// the <see cref="FindOrDefault"/> that loads the state a change applies to, and the root key lookup of
    /// <see cref="TryFindRootKeyByChildValue"/>. <see cref="FindOrDefault"/> is also how a query reads one instance
    /// by key, and the sink cannot tell that read from the pipeline's, so it follows the replay for both. Reading
    /// instances for queries goes to the primary table through <see cref="QueriedTableNameFor"/>, so readers keep
    /// seeing the previous state until the replay is promoted.
    /// </remarks>
    string ActiveTableName => IsReplaying ? ReplayTableNameFor(_tableName) : _tableName;

    bool IsReplaying => _replayingTables.IsReplaying(_eventStoreName, _namespace, _tableName);

    /// <inheritdoc/>
    public async Task<ExpandoObject?> FindOrDefault(Key key)
    {
        await using var scope = await OpenActiveScope();
        var id = await ResolveStoredId(scope, key);
        var entity = await scope.DbContext.Entries.AsNoTracking().FirstOrDefaultAsync(BuildIdPredicate(id));
        return entity is null ? null : MaterializeExpando(entity);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> ApplyChanges(
        Key key,
        IChangeset<AppendedEvent, ExpandoObject> changeset,
        EventSequenceNumber eventSequenceNumber) =>
        ApplyChanges(key, changeset, eventSequenceNumber, SinkWriteMode.Always);

    /// <inheritdoc/>
    /// <remarks>
    /// <see cref="SinkWriteMode.OnlyWhenAdvancingWatermark"/> is evaluated against the entity loaded into this
    /// scope's change tracker and decided in process, so — unlike the MongoDB filter — it is a read-then-decide
    /// rather than an atomic conditional update. That is sufficient because the mode is only ever requested for a
    /// read model whose documents are keyed by event source id, and every such document is written through a
    /// single Orleans activation (the observer subscriber is partitioned by event source id, and the projection
    /// pipeline additionally stripes its read-modify-write cycle on the same key). The read-modify-write the
    /// reducer and projection pipelines already perform depends on exactly that guarantee.
    /// </remarks>
    public async Task<IEnumerable<FailedPartition>> ApplyChanges(
        Key key,
        IChangeset<AppendedEvent, ExpandoObject> changeset,
        EventSequenceNumber eventSequenceNumber,
        SinkWriteMode mode)
    {
        await using var scope = await OpenActiveScope();
        var id = await ResolveStoredId(scope, key);

        if (changeset.HasBeenRemoved())
        {
            await RemoveEntry(scope, id);
            return _noFailedPartitions;
        }

        await ApplyJoinsAndChildRemovals(scope, changeset, eventSequenceNumber);

        var nonJoinedChanges = changeset.Changes
            .Where(c => c is not Joined and not ChildRemovedFromAll)
            .ToArray();
        if (nonJoinedChanges.Length == 0 || !await ShouldWriteDirectChanges(scope, id, changeset, nonJoinedChanges))
        {
            return _noFailedPartitions;
        }

        var entry = await GetOrAttachEntity(scope, GetIdString(key), key);
        if (IsSkippedByWatermark(entry, mode, eventSequenceNumber))
        {
            entry.State = EntityState.Detached;
            return _noFailedPartitions;
        }

        ApplyChangesToEntity(entry, nonJoinedChanges);
        AdvanceLastHandledSequenceNumber(entry, eventSequenceNumber);
        await SaveRecoveringFromInsertRace(scope, entry, new PendingWrite(GetIdString(key), nonJoinedChanges, eventSequenceNumber));

        return _noFailedPartitions;
    }

    async Task RemoveEntry(DbContextScope<ReadModelDbContext> scope, string id)
    {
        var toRemove = await scope.DbContext.Entries.FirstOrDefaultAsync(BuildIdPredicate(id));
        if (toRemove is not null)
        {
            scope.DbContext.Entries.Remove(toRemove);
            await scope.DbContext.SaveChangesAsync();
        }
    }

    async Task ApplyJoinsAndChildRemovals(DbContextScope<ReadModelDbContext> scope, IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber eventSequenceNumber)
    {
        foreach (var joined in changeset.Changes.OfType<Joined>())
        {
            await ApplyJoinedChange(scope, joined, eventSequenceNumber);
        }

        foreach (var childRemovedFromAll in changeset.Changes.OfType<ChildRemovedFromAll>())
        {
            await RemoveChildFromAllEntries(scope, childRemovedFromAll, eventSequenceNumber);
        }
    }

    async Task<bool> ShouldWriteDirectChanges(DbContextScope<ReadModelDbContext> scope, string id, IChangeset<AppendedEvent, ExpandoObject> changeset, Change[] nonJoinedChanges)
    {
        var joins = changeset.Changes.OfType<Joined>().ToArray();
        var onlyPropertyUpdates = nonJoinedChanges.All(c => c is PropertiesChanged<ExpandoObject>);
        if (!onlyPropertyUpdates || joins.Any(joined => joined.HasKeyedFrom))
        {
            return true;
        }

        if (joins.Any(joined => joined.ArrayIndexers.IsEmpty))
        {
            // A root join's direct all-event properties have no keyed From target. ApplyJoinedChange
            // already updated the matched rows; even an existing row at the event source id may be
            // unrelated to the join. Keep the keyed path only for a genuine From+Join event.
            return false;
        }

        if (joins.Length == 0)
        {
            return true;
        }

        // When the event was consumed by a Children Join and the only remaining changes are
        // FromEvery-style PropertiesChanged, we must not upsert a phantom row keyed on the
        // joined value (e.g. a User's GroupId becoming a fake Group). Only update if the row
        // already exists.
        return await scope.DbContext.Entries.AnyAsync(BuildIdPredicate(id));
    }

    bool IsSkippedByWatermark(EntityEntry<DynamicReadModelEntity> entry, SinkWriteMode mode, EventSequenceNumber eventSequenceNumber)
    {
        if (mode != SinkWriteMode.OnlyWhenAdvancingWatermark || !eventSequenceNumber.IsActualValue)
        {
            return false;
        }

        return !AdvancesWatermark(entry, eventSequenceNumber);
    }

    async Task SaveRecoveringFromInsertRace(DbContextScope<ReadModelDbContext> scope, EntityEntry<DynamicReadModelEntity> entry, PendingWrite write)
    {
        try
        {
            await scope.DbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex) && entry.State == EntityState.Added)
        {
            // Lost the insert race against another concurrent ApplyChanges for the same key.
            // The row now exists; detach our Added entity, reload from DB, re-apply the changes
            // to the freshly-loaded entity, and try the save again. This matches MongoDB's
            // upsert semantics — the second write becomes an UPDATE atop the first.
            entry.State = EntityState.Detached;
            var reloaded = await scope.DbContext.Entries.FirstOrDefaultAsync(BuildIdPredicate(write.Id));
            if (reloaded is null)
            {
                throw;
            }

            var reloadedEntry = scope.DbContext.Entries.Entry(reloaded);
            ApplyChangesToEntity(reloadedEntry, write.Changes);
            AdvanceLastHandledSequenceNumber(reloadedEntry, write.SequenceNumber);
            await scope.DbContext.SaveChangesAsync();
        }
    }

    ProjectedColumn? FindColumn(string name) => _columns.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.Ordinal));

    /// <inheritdoc/>
    /// <remarks>
    /// Bulk mode is a no-op for the SQL sink: every <c language="csharp">ApplyChanges</c> call commits
    /// independently through its own <see cref="DbContext"/>. The MongoDB sink batches inside
    /// a single bulk write because the engine may dispatch <c language="csharp">ApplyChanges</c>
    /// concurrently for one sink, and a shared <see cref="DbContext"/> would corrupt EF's
    /// non-thread-safe change tracker and deadlock under load. The operational guarantee bulk
    /// existed to provide — queries keep observing the previous state until the rebuild is
    /// finished — is delivered by the replay shadow-table swap in <see cref="EndReplay"/>.
    /// </remarks>
    public Task BeginBulk() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> EndBulk() => Task.FromResult<IEnumerable<FailedPartition>>([]);

    /// <inheritdoc/>
    public async Task BeginReplay(ReplayContext context)
    {
        // Replay writes go to a shadow table (replay-{tableName}); the primary table is left
        // untouched so reads from the running system keep observing the previous state until
        // the swap in EndReplay. PrepareInitialRun routes through ActiveTableName, which now
        // resolves to the replay name.
        await NamePrimaryKeyAfterPrimaryTable();
        _replayingTables.Begin(_eventStoreName, _namespace, _tableName);
        await PrepareInitialRun();
        await BeginBulk();
    }

    /// <inheritdoc/>
    public async Task ResumeReplay(ReplayContext context)
    {
        // Re-enter replay mode after an interruption without wiping the replay table — it
        // already contains the work the previous run produced before it was paused.
        _replayingTables.Begin(_eventStoreName, _namespace, _tableName);
        await BeginBulk();
    }

    /// <inheritdoc/>
    /// <exception cref="ReplayTableBecameEmpty">The rebuilt table lost its rows before a promotion attempt.</exception>
    /// <exception cref="UnverifiedReplayBackup">An existing backup cannot be distinguished from a legacy half-swap.</exception>
    public async Task<IEnumerable<FailedPartition>> EndReplay(ReplayContext context)
    {
        var failedPartitions = await EndBulk();
        try
        {
            await PerformRenameSwap(context);
        }
        finally
        {
            _replayingTables.End(_eventStoreName, _namespace, _tableName);
        }

        return failedPartitions;
    }

    /// <inheritdoc/>
    public async Task LeaveReplay()
    {
        _replayingTables.End(_eventStoreName, _namespace, _tableName);
        await EndBulk();
    }

    /// <inheritdoc/>
    public async Task Remove(ReadModelContainerName containerName)
    {
        // Do not migrate the backup being removed: a truncated logical name may now address another
        // replay's backup. Resolve ownership through the marker table on the primary's connection.
        await using var scope = await _database.ReadModelTable(_eventStoreName, _namespace, _tableName, _columns);
        await scope.DbContext.EnsureReplayPromotions();
        var database = scope.DbContext.Database;
        var physicalName = PrimaryKeyNames.TableIdentifier(database.GetDatabaseType(), containerName.Value);
        var revertName = containerName.Value;
        await database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await database.BeginTransactionAsync();
            var belongsToAnotherReplay = await scope.DbContext.ReplayPromotions
                .AnyAsync(promotion => promotion.BackupTableName == physicalName && promotion.RevertContainerName != revertName);
            if (!belongsToAnotherReplay)
            {
                await ExecuteDdl(scope, BuildDropSql(scope, containerName.Value));
            }

            // Retention calls Remove for each unique logical revert name, even after the read model's
            // container changes. Keep completion evidence until then, even if PostgreSQL reused the
            // physical name for a newer replay. Both operations
            // are idempotent if a commit acknowledgment is lost and the strategy repeats this body.
            await scope.DbContext.ReplayPromotions.Where(promotion => promotion.RevertContainerName == revertName).ExecuteDeleteAsync();
            await transaction.CommitAsync();
        });
    }

    /// <inheritdoc/>
    public async Task PrepareInitialRun()
    {
        await using var scope = await _database.ReadModelTable(_eventStoreName, _namespace, ActiveTableName, _columns);
        scope.DbContext.Entries.RemoveRange(scope.DbContext.Entries);
        await scope.DbContext.SaveChangesAsync();
    }

    static string ReplayTableNameFor(string baseName) => $"replay-{baseName}";

    /// <summary>
    /// Opens a fresh scope routed at <see cref="ActiveTableName"/>. Caller owns the scope and
    /// is responsible for saving and disposing it.
    /// </summary>
    Task<DbContextScope<ReadModelDbContext>> OpenActiveScope() =>
        _database.ReadModelTable(_eventStoreName, _namespace, ActiveTableName, _columns);

    /// <inheritdoc/>
    public Task EnsureIndexes() => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task<Option<Key>> TryFindRootKeyByChildValue(PropertyPath childPropertyPath, object childValue)
    {
        if (childValue is null)
        {
            return Option<Key>.None();
        }

        var pathSegments = childPropertyPath.Segments.ToArray();
        if (pathSegments.Length == 0)
        {
            return Option<Key>.None();
        }

        var rootColumn = FindColumn(pathSegments[0].Value);
        var keyColumn = _columns.FirstOrDefault(c => c.IsKey);
        if (rootColumn?.IsJson != true || keyColumn is null)
        {
            return Option<Key>.None();
        }

        await using var scope = await OpenActiveScope();
        var entries = await scope.DbContext.Entries.AsNoTracking().ToListAsync();
        foreach (var entry in entries)
        {
            var keyValue = ContainsChildValue(entry, rootColumn, pathSegments, childValue) ? entry[keyColumn.Name] : null;
            if (keyValue is not null)
            {
                return new Option<Key>(new Key(keyValue, ArrayIndexers.NoIndexers));
            }
        }

        return Option<Key>.None();
    }

    static bool ContainsChildValue(DynamicReadModelEntity entry, ProjectedColumn rootColumn, IPropertyPathSegment[] pathSegments, object childValue)
    {
        if (entry[rootColumn.Name] is not string jsonValue || string.IsNullOrEmpty(jsonValue))
        {
            return false;
        }

        var collection = DeserializeJsonColumn(jsonValue);
        return collection is not null && TryFindValueInDeserialized(collection, pathSegments, 1, childValue);
    }

    /// <inheritdoc/>
    public async Task<ReadModelInstances> GetInstances(ReadModelContainerName? occurrence = null, int skip = 0, int take = 50)
    {
        var containerName = QueriedTableNameFor(occurrence);
        await using var scope = await _database.ReadModelTable(_eventStoreName, _namespace, containerName, _columns);
        var totalCount = await scope.DbContext.Entries.CountAsync();
        var entries = await OrderByKey(scope.DbContext.Entries.AsNoTracking()).Skip(skip).Take(take).ToListAsync();
        return new ReadModelInstances(entries.Select(MaterializeExpando), totalCount);
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<ExpandoObject>> ObserveInstances(ReadModelContainerName? occurrence = null, int skip = 0, int take = 50)
    {
        // Watch notifies projection changes, but misses reducer writes, removals, and the replay
        // rename swap. Poll stored state like other SQL live queries to observe all of these changes.
        // The table is resolved on every poll rather than once for the subscription, so a subscription
        // never stays bound to a table a replay promotion has renamed away.
        async Task<IEnumerable<ObservedPage>> ReadPage()
        {
            var containerName = QueriedTableNameFor(occurrence);
            await using var scope = await _database.ReadModelTable(_eventStoreName, _namespace, containerName, _columns);
            var totalCount = await scope.DbContext.Entries.CountAsync();
            var entries = await OrderByKey(scope.DbContext.Entries.AsNoTracking()).Skip(skip).Take(take).ToArrayAsync();
            return [new ObservedPage(totalCount, entries)];
        }

        return LiveQuery.Observe(ReadPage, _database.LiveQueryPollingInterval, ObservedPageComparer.Instance)
            .Select(pages => pages.Single().Entries.Select(MaterializeExpando).ToArray().AsEnumerable())
            .Catch<IEnumerable<ExpandoObject>, Exception>(error =>
                Observable.Throw<IEnumerable<ExpandoObject>>(new FailedToObserveReadModelInstances(TypeId, _readModelIdentifier, QueriedTableNameFor(occurrence), error)));
    }

    /// <summary>
    /// Resolves the table instances are read from for a query.
    /// </summary>
    /// <param name="occurrence">The explicitly requested <see cref="ReadModelContainerName"/>, if any.</param>
    /// <returns>The name of the table to read.</returns>
    /// <remarks>
    /// An explicit occurrence reads that specific table, such as the revert backup. Otherwise the primary table
    /// is read even while a replay is in progress: the replay rebuilds its own table and the primary one keeps
    /// the previous state until EndReplay promotes the rebuilt table, so queries never see a read model that is
    /// empty or only partly rebuilt.
    /// </remarks>
    string QueriedTableNameFor(ReadModelContainerName? occurrence) => occurrence?.Value ?? _tableName;

    IQueryable<DynamicReadModelEntity> OrderByKey(IQueryable<DynamicReadModelEntity> entries)
    {
        var keyColumn = _columns.First(column => column.IsKey);
        var entry = Expression.Parameter(typeof(DynamicReadModelEntity), "entry");
        var key = Expression.Call(
            typeof(EF).GetMethod(nameof(EF.Property))!.MakeGenericMethod(keyColumn.ClrType),
            entry,
            Expression.Constant(keyColumn.Name));
        var selector = Expression.Lambda(key, entry);
        var orderBy = Expression.Call(
            typeof(Queryable),
            nameof(Queryable.OrderBy),
            [typeof(DynamicReadModelEntity), keyColumn.ClrType],
            entries.Expression,
            Expression.Quote(selector));
        return entries.Provider.CreateQuery<DynamicReadModelEntity>(orderBy);
    }

    async Task ApplyJoinedChange(DbContextScope<ReadModelDbContext> scope, Joined joined, EventSequenceNumber eventSequenceNumber)
    {
        var (segments, joinValue) = !joined.ArrayIndexers.IsEmpty
            ? GetChildJoinFilter(joined)
            : (joined.OnProperty.Segments.ToArray(), joined.Key);
        if (joinValue is null || segments.Length == 0)
        {
            return;
        }

        var rootColumn = FindColumn(segments[0].Value);
        if (rootColumn is null)
        {
            return;
        }

        var entries = await scope.DbContext.Entries.ToListAsync();
        var modified = false;
        foreach (var entry in entries.Where(entry => EntryMatchesJoin(entry, rootColumn, segments, joinValue)))
        {
            ApplyChangesToEntity(scope.DbContext.Entries.Entry(entry), joined.Changes);
            AdvanceLastHandledSequenceNumber(scope.DbContext.Entries.Entry(entry), eventSequenceNumber);
            modified = true;
        }

        if (modified)
        {
            await scope.DbContext.SaveChangesAsync();
        }
    }

    async Task RemoveChildFromAllEntries(DbContextScope<ReadModelDbContext> scope, ChildRemovedFromAll childRemoved, EventSequenceNumber eventSequenceNumber)
    {
        var column = FindColumn(childRemoved.ChildrenProperty.Segments.First().Value);
        if (column?.IsJson != true)
        {
            return;
        }

        var removal = new ChildRemoval(childRemoved.ChildrenProperty.Segments.ToArray(), childRemoved.IdentifiedByProperty, childRemoved.Key);
        var entries = await scope.DbContext.Entries.ToListAsync();
        var modified = false;
        foreach (var entry in entries)
        {
            if (!TryRemoveChildren(entry, column, removal))
            {
                continue;
            }

            var trackedEntry = scope.DbContext.Entries.Entry(entry);
            trackedEntry.Property(column.Name).IsModified = true;
            AdvanceLastHandledSequenceNumber(trackedEntry, eventSequenceNumber);
            modified = true;
        }

        if (modified)
        {
            await scope.DbContext.SaveChangesAsync();
        }
    }

    static bool TryRemoveChildren(DynamicReadModelEntity entry, ProjectedColumn column, ChildRemoval removal)
    {
        if (entry[column.Name] is not string jsonValue || string.IsNullOrEmpty(jsonValue))
        {
            return false;
        }

        var content = DeserializeJsonColumn(jsonValue);
        if (!RemoveChildrenFromPath(content, 1, removal))
        {
            return false;
        }

        entry[column.Name] = SerializeJsonColumn(content, column);
        return true;
    }

    async Task<EntityEntry<DynamicReadModelEntity>> GetOrAttachEntity(DbContextScope<ReadModelDbContext> scope, string id, Key key)
    {
        var existing = await scope.DbContext.Entries.FirstOrDefaultAsync(BuildIdPredicate(id));
        if (existing is not null)
        {
            return scope.DbContext.Entries.Entry(existing);
        }

        if (await RenameLegacyEntity(scope, id, key) is { } renamed)
        {
            return renamed;
        }

        await RecommendReplayForLegacyRows(scope, key);

        var fresh = new DynamicReadModelEntity();
        SeedDefaults(fresh);
        var keyColumn = _columns.FirstOrDefault(c => c.IsKey);
        if (keyColumn is not null)
        {
            // Composite keys arrive as an ExpandoObject; the table's key column is a single typed
            // value, so use the GetIdString representation that BuildIdPredicate searches by — the
            // raw ExpandoObject can't be coerced and EF would fail with InvalidCastException at
            // INSERT.
            var keyValue = key.Value is ExpandoObject ? id : key.Value;
            fresh[keyColumn.Name] = CoerceForColumn(keyColumn, keyValue);
        }

        scope.DbContext.Entries.Add(fresh);
        return scope.DbContext.Entries.Entry(fresh);
    }

    ExpandoObject MaterializeExpando(DynamicReadModelEntity entity)
    {
        // Build a JsonObject from the row's column values so that the schema-aware
        // IExpandoObjectConverter can reconstitute concept-typed identifiers (e.g. UserId =>
        // ConceptAs<Guid>) when materializing the read model. Without schema-driven conversion,
        // every value comes back as a plain string and downstream FindByKey calls — which use
        // .Equals on the original ConceptAs identifier — silently miss every match. Funnel the
        // raw column values through JsonSerializer to get a JsonNode-shaped JSON document that
        // matches what the converter expects to parse.
        var dict = ToJsonNodeValues(entity);
        var json = (JsonObject)JsonSerializer.SerializeToNode(dict, ReadModelDbContext.JsonSerializerOptions)!;
        var result = _expandoObjectConverter.ToExpandoObject(json, _schema);
        var resultDictionary = (IDictionary<string, object?>)result;
        ApplyInitializedMarker(entity, resultDictionary, dict);
        ReattachSinkOwnedValues(entity, resultDictionary);

        return result;
    }

    Dictionary<string, object?> ToJsonNodeValues(DynamicReadModelEntity entity)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var column in _columns)
        {
            if (!entity.TryGetValue(column.Name, out var value) || value is null)
            {
                continue;
            }

            dict[column.Name] = column.IsKey && IsCompositeKeyColumn(column) && value is string encodedKey && CompositeKeyEncoding.TryDecode(encodedKey, out var keyParts)
                ? keyParts
                : ToJsonNode(column, value);
        }

        return dict;
    }

    bool IsCompositeKeyColumn(ProjectedColumn column) =>
        _schema.Properties.TryGetValue(column.Name, out var property) && property.Type.HasFlag(JsonObjectType.Object) && string.IsNullOrEmpty(property.Format);

    static JsonNode? ToJsonNode(ProjectedColumn column, object value)
    {
        if (column.IsJson && value is string jsonText)
        {
            return string.IsNullOrEmpty(jsonText) ? null : JsonNode.Parse(jsonText);
        }

        return JsonSerializer.SerializeToNode(value, ReadModelDbContext.JsonSerializerOptions);
    }

    void ApplyInitializedMarker(DynamicReadModelEntity entity, IDictionary<string, object?> resultDictionary, Dictionary<string, object?> presentValues)
    {
        if (!entity.TryGetValue(WellKnownProperties.ReadModelInstanceInitialized, out var initialized) || initialized is not bool isInitialized)
        {
            return;
        }

        // Schema conversion supplies CLR defaults for absent numeric properties. A placeholder must
        // retain absence so the root event can fill in the projection's initial values instead.
        if (!isInitialized)
        {
            foreach (var column in _columns.Where(column => !presentValues.ContainsKey(column.Name)))
            {
                resultDictionary.Remove(column.Name);
            }
        }

        resultDictionary[WellKnownProperties.ReadModelInstanceInitialized] = isInitialized;
    }

    static void ReattachSinkOwnedValues(DynamicReadModelEntity entity, IDictionary<string, object?> resultDictionary)
    {
        // The schema-aware conversion only carries schema-declared properties. The compliance subject is a
        // sink-owned column that lives outside the read model schema, so re-attach it onto the materialized
        // instance — both the read path (releasing PII on read) and the projection pipeline (decrypting the
        // initial state before re-encrypting) key their release off this subject; without it PII stays
        // encrypted on read and the next event double-encrypts the already-encrypted carry-over state.
        if (entity.TryGetValue(WellKnownProperties.Subject, out var subject) && subject is string subjectValue)
        {
            resultDictionary[WellKnownProperties.Subject] = subjectValue;
        }

        if (TryReadSubjects(entity) is { } subjectsDocument)
        {
            resultDictionary[WellKnownProperties.Subjects] = subjectsDocument;
        }
    }

    static ExpandoObject? TryReadSubjects(DynamicReadModelEntity entity)
    {
        if (!entity.TryGetValue(WellKnownProperties.Subjects, out var subjects) || subjects is not string { Length: > 0 } subjectsValue)
        {
            return null;
        }

        return JsonSerializer.Deserialize<ExpandoObject>(subjectsValue, ReadModelDbContext.JsonSerializerOptions);
    }

    System.Linq.Expressions.Expression<Func<DynamicReadModelEntity, bool>> BuildIdPredicate(string id)
    {
        var keyColumn = _columns.FirstOrDefault(c => c.IsKey);
        if (keyColumn is null)
        {
            return _ => false;
        }

        var name = keyColumn.Name;
        if (keyColumn.ClrType == typeof(Guid) && Guid.TryParse(id, out var guid))
        {
            return e => (Guid)e[name]! == guid;
        }

        return e => (string)e[name]! == id;
    }

    bool EntryMatchesJoin(DynamicReadModelEntity entity, ProjectedColumn rootColumn, IPropertyPathSegment[] segments, object joinValue)
    {
        if (rootColumn.IsJson)
        {
            if (entity[rootColumn.Name] is not string json || string.IsNullOrEmpty(json))
            {
                return false;
            }

            var content = DeserializeJsonColumn(json);
            return TryFindValueInDeserialized(content, segments, 1, joinValue);
        }

        if (segments.Length == 1)
        {
            var stored = entity[rootColumn.Name];
            return CompareLoose(stored, joinValue);
        }

        return false;
    }

    static (IPropertyPathSegment[] Segments, object? Value) GetChildJoinFilter(Joined joined)
    {
        var lastIndexer = joined.ArrayIndexers.All.Last();
        var combined = lastIndexer.ArrayProperty + lastIndexer.IdentifierProperty;
        return (combined.Segments.ToArray(), lastIndexer.Identifier);
    }

    static bool IsUniqueViolation(DbUpdateException ex)
    {
        var inner = ex.InnerException?.GetType().FullName ?? string.Empty;
        var message = ex.InnerException?.Message ?? string.Empty;
        return _uniqueViolations.Any(violation =>
            inner.EndsWith(violation.ExceptionSuffix, StringComparison.Ordinal) &&
            violation.MessageFragments.Any(fragment => message.Contains(fragment, StringComparison.OrdinalIgnoreCase)));
    }

    sealed record PendingWrite(string Id, Change[] Changes, EventSequenceNumber SequenceNumber);

    sealed record ObservedPage(long TotalCount, DynamicReadModelEntity[] Entries);

    sealed class ObservedPageComparer : IEqualityComparer<ObservedPage>
    {
        public static readonly ObservedPageComparer Instance = new();

        public bool Equals(ObservedPage? x, ObservedPage? y) =>
            ReferenceEquals(x, y) ||
            (x is not null && y is not null && x.TotalCount == y.TotalCount &&
             x.Entries.SequenceEqual(y.Entries, DynamicReadModelEntityComparer.Instance));

        public int GetHashCode(ObservedPage obj) => HashCode.Combine(obj.TotalCount, obj.Entries.Length);
    }

    sealed class DynamicReadModelEntityComparer : IEqualityComparer<DynamicReadModelEntity>
    {
        public static readonly DynamicReadModelEntityComparer Instance = new();

        public bool Equals(DynamicReadModelEntity? x, DynamicReadModelEntity? y) =>
            ReferenceEquals(x, y) ||
            (x is not null && y is not null && x.Count == y.Count &&
             x.All(column => y.TryGetValue(column.Key, out var value) && Equals(column.Value, value)));

        public int GetHashCode(DynamicReadModelEntity obj) => obj.Count;
    }
}
