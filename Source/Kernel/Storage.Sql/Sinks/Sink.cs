// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using System.Globalization;
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
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

#pragma warning disable SA1202 // Private helpers grouped next to the public methods they back.
#pragma warning disable SA1204 // Static helpers grouped at the bottom for readability.

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// SQL implementation of <see cref="ISink"/> backed by a per-read-model table whose column shape is
/// derived from the read model's <see cref="JsonSchema"/>: each leaf
/// property becomes a real typed column, collections and nested objects become a single JSON column
/// (<c>jsonb</c> on PostgreSQL, <c>nvarchar(max)</c> on SQL Server, <c>TEXT</c> on SQLite). Changes
/// from the projection engine are translated into per-column updates on a tracked
/// <see cref="DynamicReadModelEntity"/>; EF's change tracker turns those into <c>UPDATE</c>s that
/// only touch the modified columns — matching MongoDB's <c>$set</c> semantics and removing the
/// stale-snapshot overwrite class of bug that whole-document upserts suffered from.
/// </summary>
public class Sink : ISink
{
    static readonly IEnumerable<FailedPartition> _noFailedPartitions = [];

    readonly Concepts.EventStoreName _eventStoreName;
    readonly Concepts.EventStoreNamespaceName _namespace;
    readonly IDatabase _database;
    readonly IExpandoObjectConverter _expandoObjectConverter;
    readonly JsonSchema _schema;
    readonly string _tableName;
    readonly IReadOnlyList<ProjectedColumn> _columns;

    volatile bool _isReplaying;

    /// <summary>
    /// Initializes a new instance of the <see cref="Sink"/> class.
    /// </summary>
    /// <param name="eventStoreName">The <see cref="Concepts.EventStoreName"/> the sink is for.</param>
    /// <param name="namespace">The <see cref="Concepts.EventStoreNamespaceName"/> the sink is for.</param>
    /// <param name="readModel">The <see cref="ReadModelDefinition"/> the sink is for.</param>
    /// <param name="database">The <see cref="IDatabase"/> for accessing SQL storage.</param>
    /// <param name="expandoObjectConverter">The schema-aware <see cref="IExpandoObjectConverter"/>.</param>
    public Sink(
        Concepts.EventStoreName eventStoreName,
        Concepts.EventStoreNamespaceName @namespace,
        ReadModelDefinition readModel,
        IDatabase database,
        IExpandoObjectConverter expandoObjectConverter)
    {
        _eventStoreName = eventStoreName;
        _namespace = @namespace;
        _database = database;
        _expandoObjectConverter = expandoObjectConverter;
        _schema = readModel.GetSchemaForLatestGeneration();
        _tableName = readModel.ContainerName.Value;
        _columns = ProjectedColumns.ForSchema(_schema);
    }

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.SQL;

    /// <summary>
    /// Gets the table name the sink is currently writing to. Resolves to <c>replay-{tableName}</c>
    /// while a replay is in progress, and to the primary table name otherwise. EndReplay swaps the
    /// two so the running system observes the rebuilt state atomically.
    /// </summary>
    string ActiveTableName => _isReplaying ? ReplayTableNameFor(_tableName) : _tableName;

    /// <inheritdoc/>
    public async Task<ExpandoObject?> FindOrDefault(Key key)
    {
        var id = GetIdString(key);
        await using var scope = await OpenActiveScope();
        var entity = await scope.DbContext.Entries.AsNoTracking().FirstOrDefaultAsync(BuildIdPredicate(id));
        return entity is null ? null : MaterializeExpando(entity);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<FailedPartition>> ApplyChanges(
        Key key,
        IChangeset<AppendedEvent, ExpandoObject> changeset,
        EventSequenceNumber eventSequenceNumber)
    {
        var id = GetIdString(key);
        await using var scope = await OpenActiveScope();

        if (changeset.HasBeenRemoved())
        {
            var toRemove = await scope.DbContext.Entries.FirstOrDefaultAsync(BuildIdPredicate(id));
            if (toRemove is not null)
            {
                scope.DbContext.Entries.Remove(toRemove);
                await scope.DbContext.SaveChangesAsync();
            }

            return _noFailedPartitions;
        }

        foreach (var joined in changeset.Changes.OfType<Joined>())
        {
            await ApplyJoinedChange(scope, joined, eventSequenceNumber);
        }

        foreach (var childRemovedFromAll in changeset.Changes.OfType<ChildRemovedFromAll>())
        {
            await RemoveChildFromAllEntries(scope, childRemovedFromAll, eventSequenceNumber);
        }

        var nonJoinedChanges = changeset.Changes
            .Where(c => c is not Joined and not ChildRemovedFromAll)
            .ToArray();
        if (nonJoinedChanges.Length == 0)
        {
            return _noFailedPartitions;
        }

        var hasJoined = changeset.Changes.OfType<Joined>().Any();
        var onlyPropertyUpdates = nonJoinedChanges.All(c => c is PropertiesChanged<ExpandoObject>);
        if (hasJoined && onlyPropertyUpdates)
        {
            // When the event was consumed by a Children Join and the only remaining changes are
            // FromEvery-style PropertiesChanged, we must not upsert a phantom row keyed on the
            // joined value (e.g. a User's GroupId becoming a fake Group). Only update if the row
            // already exists.
            var exists = await scope.DbContext.Entries.AnyAsync(BuildIdPredicate(id));
            if (!exists)
            {
                return _noFailedPartitions;
            }
        }

        var entry = await GetOrAttachEntity(scope, id, key);
        ApplyChangesToEntity(entry, nonJoinedChanges);
        AdvanceLastHandledSequenceNumber(entry, eventSequenceNumber);
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
            var reloaded = await scope.DbContext.Entries.FirstOrDefaultAsync(BuildIdPredicate(id));
            if (reloaded is null)
            {
                throw;
            }

            var reloadedEntry = scope.DbContext.Entries.Entry(reloaded);
            ApplyChangesToEntity(reloadedEntry, nonJoinedChanges);
            AdvanceLastHandledSequenceNumber(reloadedEntry, eventSequenceNumber);
            await scope.DbContext.SaveChangesAsync();
        }

        return _noFailedPartitions;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Bulk mode is a no-op for the SQL sink: every <see cref="ApplyChanges"/> call commits
    /// independently through its own <see cref="DbContext"/>. The MongoDB sink batches inside
    /// a single bulk write because the engine may dispatch <see cref="ApplyChanges"/>
    /// concurrently for one sink, and a shared <see cref="DbContext"/> would corrupt EF's
    /// non-thread-safe change tracker and deadlock under load. The operational guarantee bulk
    /// existed to provide — queries keep observing the previous state until the rebuild is
    /// finished — is delivered by the replay shadow-table swap in <see cref="EndReplay"/>.
    /// </remarks>
    public Task BeginBulk() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task EndBulk() => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task BeginReplay(ReplayContext context)
    {
        // Replay writes go to a shadow table (replay-{tableName}); the primary table is left
        // untouched so reads from the running system keep observing the previous state until
        // the swap in EndReplay. PrepareInitialRun routes through ActiveTableName, which now
        // resolves to the replay name.
        _isReplaying = true;
        await PrepareInitialRun();
        await BeginBulk();
    }

    /// <inheritdoc/>
    public async Task ResumeReplay(ReplayContext context)
    {
        // Re-enter replay mode after an interruption without wiping the replay table — it
        // already contains the work the previous run produced before it was paused.
        _isReplaying = true;
        await BeginBulk();
    }

    /// <inheritdoc/>
    public async Task EndReplay(ReplayContext context)
    {
        await EndBulk();
        try
        {
            await PerformRenameSwap(context);
        }
        finally
        {
            _isReplaying = false;
        }
    }

    /// <inheritdoc/>
    public async Task Remove(ReadModelContainerName containerName)
    {
        await using var scope = await _database.ReadModelTable(_eventStoreName, _namespace, containerName.Value, _columns);
        var sqlGenerationHelper = scope.DbContext.GetService<ISqlGenerationHelper>();
        var delimited = sqlGenerationHelper.DelimitIdentifier(containerName.Value);
#pragma warning disable EF1002 // delimited is sanitized by ISqlGenerationHelper.
        await scope.DbContext.Database.ExecuteSqlRawAsync($"DROP TABLE IF EXISTS {delimited}");
#pragma warning restore EF1002
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

    async Task PerformRenameSwap(ReplayContext context)
    {
        var replayName = ReplayTableNameFor(_tableName);

        // Open the scope on the replay table so the DbContext we use for DDL is bound to a
        // connection that definitely exists post-replay. Any access through the standard
        // ReadModelTable path goes through EnsureTableExists, which would recreate the
        // primary table if it had already been renamed away — using the replay table avoids
        // that race.
        await using var scope = await _database.ReadModelTable(_eventStoreName, _namespace, replayName, _columns);

        var replayHasRows = await scope.DbContext.Entries.AsNoTracking().AnyAsync();
        if (!replayHasRows)
        {
            // Replay produced no writes (e.g. there were no events for this projection yet).
            // Drop the empty replay table and keep the primary untouched — turning a transient
            // race into permanent data loss is the very thing the shadow-table dance exists
            // to prevent.
            await ExecuteDdl(scope, BuildDropSql(scope, replayName));
            return;
        }

        var databaseType = scope.DbContext.Database.GetDatabaseType();
        var revertName = context.RevertContainerName.Value;

        // Drop any stale backup with the same revert name first (a previous EndReplay may have
        // left one behind), then rename primary -> revert (preserved for downgrade) and
        // replay -> primary.
        await ExecuteDdl(scope, BuildDropSql(scope, revertName));
        await ExecuteDdl(scope, BuildRenameSql(scope, databaseType, _tableName, revertName));
        await ExecuteDdl(scope, BuildRenameSql(scope, databaseType, replayName, _tableName));
    }

    static async Task ExecuteDdl(DbContextScope<ReadModelDbContext> scope, string sql)
    {
#pragma warning disable EF1002 // sql is built from delimited identifiers via ISqlGenerationHelper.
        await scope.DbContext.Database.ExecuteSqlRawAsync(sql);
#pragma warning restore EF1002
    }

    static string BuildDropSql(DbContextScope<ReadModelDbContext> scope, string tableName)
    {
        var sqlHelper = scope.DbContext.GetService<ISqlGenerationHelper>();
        return $"DROP TABLE IF EXISTS {sqlHelper.DelimitIdentifier(tableName)}";
    }

    static string BuildRenameSql(DbContextScope<ReadModelDbContext> scope, DatabaseType databaseType, string oldName, string newName)
    {
        if (databaseType == DatabaseType.SqlServer)
        {
            // sp_rename takes name strings, not identifiers. The new name must be the unqualified
            // table name — Microsoft's docs are explicit on this.
            var oldEscaped = oldName.Replace("'", "''", StringComparison.Ordinal);
            var newEscaped = newName.Replace("'", "''", StringComparison.Ordinal);
            return $"EXEC sp_rename N'{oldEscaped}', N'{newEscaped}'";
        }

        var sqlHelper = scope.DbContext.GetService<ISqlGenerationHelper>();
        return $"ALTER TABLE {sqlHelper.DelimitIdentifier(oldName)} RENAME TO {sqlHelper.DelimitIdentifier(newName)}";
    }

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

        var rootSegmentName = pathSegments[0].Value;
        var rootColumn = _columns.FirstOrDefault(c => string.Equals(c.Name, rootSegmentName, StringComparison.Ordinal));
        if (rootColumn?.IsJson != true)
        {
            return Option<Key>.None();
        }

        var keyColumn = _columns.FirstOrDefault(c => c.IsKey);
        if (keyColumn is null)
        {
            return Option<Key>.None();
        }

        await using var scope = await OpenActiveScope();
        var entries = await scope.DbContext.Entries.AsNoTracking().ToListAsync();
        foreach (var entry in entries)
        {
            if (entry[rootColumn.Name] is not string jsonValue || string.IsNullOrEmpty(jsonValue))
            {
                continue;
            }

            var collection = DeserializeJsonColumn(jsonValue);
            if (collection is null)
            {
                continue;
            }

            if (TryFindValueInDeserialized(collection, pathSegments, 1, childValue))
            {
                var keyValue = entry[keyColumn.Name];
                if (keyValue is null)
                {
                    continue;
                }

                return new Option<Key>(new Key(keyValue, ArrayIndexers.NoIndexers));
            }
        }

        return Option<Key>.None();
    }

    /// <inheritdoc/>
    public async Task<ReadModelInstances> GetInstances(ReadModelContainerName? occurrence = null, int skip = 0, int take = 50)
    {
        // An explicit occurrence reads that specific table (e.g. the revert backup). Otherwise
        // default to ActiveTableName so that during replay, queries observe the in-progress
        // shadow table — matching MongoDB's Collection routing.
        var containerName = occurrence?.Value ?? ActiveTableName;
        await using var scope = await _database.ReadModelTable(_eventStoreName, _namespace, containerName, _columns);
        var totalCount = await scope.DbContext.Entries.CountAsync();
        var entries = await scope.DbContext.Entries.AsNoTracking().Skip(skip).Take(take).ToListAsync();
        return new ReadModelInstances(entries.Select(MaterializeExpando), totalCount);
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<ExpandoObject>> ObserveInstances(ReadModelContainerName? occurrence = null, int skip = 0, int take = 50)
    {
        var containerName = occurrence?.Value ?? ActiveTableName;

        // Return an observable that transforms Entity Framework change tracking into instance collections
        return Observable.Create<IEnumerable<ExpandoObject>>(async observer =>
        {
            // Create a scope that will live for the duration of the subscription
            var scope = await _database.ReadModelTable(_eventStoreName, _namespace, containerName, _columns);

            try
            {
                // Get initial instances
                var initialEntries = await scope.DbContext.Entries.AsNoTracking().Skip(skip).Take(take).ToListAsync();
                observer.OnNext(initialEntries.Select(MaterializeExpando));

                // Subscribe to changes using Arc's Observe extension
                var changeSubscription = scope.DbContext.Entries.Observe().Subscribe(
                    allEntries =>
                    {
                        // Re-query with skip/take when changes occur
                        observer.OnNext(allEntries.Skip(skip).Take(take).Select(MaterializeExpando));
                    },
                    observer.OnError,
                    observer.OnCompleted);

                // Return a disposable that cleans up both the subscription and the scope
                return new ObservableInstancesDisposable(changeSubscription, scope);
            }
            catch
            {
                // If anything goes wrong, dispose the scope immediately
                await scope.DisposeAsync();
                throw;
            }
        });
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

        var columnName = segments[0].Value;
        var rootColumn = _columns.FirstOrDefault(c => string.Equals(c.Name, columnName, StringComparison.Ordinal));
        if (rootColumn is null)
        {
            return;
        }

        var entries = await scope.DbContext.Entries.ToListAsync();
        var modified = false;
        foreach (var entry in entries)
        {
            if (!EntryMatchesJoin(entry, rootColumn, segments, joinValue))
            {
                continue;
            }

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
        var childrenColumnName = childRemoved.ChildrenProperty.Segments.First().Value;
        var column = _columns.FirstOrDefault(c => string.Equals(c.Name, childrenColumnName, StringComparison.Ordinal));
        if (column?.IsJson != true)
        {
            return;
        }

        var identifierSegment = childRemoved.IdentifiedByProperty.LastSegment.Value;
        var keyAsString = childRemoved.Key?.ToString();

        var entries = await scope.DbContext.Entries.ToListAsync();
        var modified = false;
        foreach (var entry in entries)
        {
            if (entry[column.Name] is not string jsonValue || string.IsNullOrEmpty(jsonValue))
            {
                continue;
            }

            if (DeserializeJsonColumn(jsonValue) is not IList<object?> collection)
            {
                continue;
            }

            var retained = new List<object?>(collection.Count);
            var didRemove = false;
            foreach (var item in collection)
            {
                if (item is IDictionary<string, object?> dict
                    && dict.TryGetValue(identifierSegment, out var value)
                    && (Equals(value, childRemoved.Key) || (value is not null && value.ToString() == keyAsString)))
                {
                    didRemove = true;
                    continue;
                }

                retained.Add(item);
            }

            if (!didRemove)
            {
                continue;
            }

            entry[column.Name] = SerializeJsonColumn(retained, column);
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

    void ApplyChangesToEntity(EntityEntry<DynamicReadModelEntity> entry, IEnumerable<Change> changes)
    {
        foreach (var change in changes)
        {
            switch (change)
            {
                case PropertiesChanged<ExpandoObject> propertiesChanged:
                    ApplyPropertyChanges(entry, propertiesChanged);
                    break;

                case ChildAdded childAdded:
                    ApplyChildAdded(entry, childAdded);
                    break;

                case ChildRemoved childRemoved:
                    ApplyChildRemoved(entry, childRemoved);
                    break;

                case NestedCleared nestedCleared:
                    ApplyNestedCleared(entry, nestedCleared);
                    break;

                case Joined joined:
                    ApplyChangesToEntity(entry, joined.Changes);
                    break;

                case ResolvedJoin resolvedJoin:
                    ApplyChangesToEntity(entry, resolvedJoin.Changes);
                    break;
            }
        }
    }

    void ApplyPropertyChanges(EntityEntry<DynamicReadModelEntity> entry, PropertiesChanged<ExpandoObject> propertiesChanged)
    {
        foreach (var difference in propertiesChanged.Differences)
        {
            ApplySingleDifference(entry, difference);
        }
    }

    void ApplySingleDifference(EntityEntry<DynamicReadModelEntity> entry, PropertyDifference difference)
    {
        var firstSegment = difference.PropertyPath.Segments.FirstOrDefault()?.Value;
        if (firstSegment is null)
        {
            return;
        }

        var column = _columns.FirstOrDefault(c => string.Equals(c.Name, firstSegment, StringComparison.Ordinal));
        if (column is null)
        {
            return;
        }

        if (!column.IsJson)
        {
            if (difference.PropertyPath.Segments.Count() != 1)
            {
                return;
            }

            SetTypedColumn(entry, column, difference.Changed);
            return;
        }

        var current = LoadJsonColumnState(entry, column);
        var indexers = difference.ArrayIndexers ?? ArrayIndexers.NoIndexers;
        var subPath = BuildSubPath(difference.PropertyPath);
        if (!subPath.Segments.Any())
        {
            entry.Entity[column.Name] = SerializeJsonColumn(UnwrapForJson(difference.Changed), column);
        }
        else
        {
            var rebuilt = ApplyDifferenceToJsonRoot(current, column, subPath, indexers, difference.Changed);
            entry.Entity[column.Name] = SerializeJsonColumn(rebuilt, column);
        }

        entry.Property(column.Name).IsModified = true;
    }

    void ApplyChildAdded(EntityEntry<DynamicReadModelEntity> entry, ChildAdded childAdded)
    {
        var pathSegments = childAdded.ChildrenProperty.Segments.ToArray();
        if (pathSegments.Length == 0)
        {
            return;
        }

        var columnName = pathSegments[0].Value;
        var column = _columns.FirstOrDefault(c => string.Equals(c.Name, columnName, StringComparison.Ordinal));
        if (column?.IsJson != true)
        {
            return;
        }

        var current = LoadJsonColumnState(entry, column);
        var unwrappedState = UnwrapForJson(childAdded.State);

        var identifierName = childAdded.IdentifiedByProperty.LastSegment.Value;
        var identifierValue = UnwrapConceptValue(childAdded.Key);
        if (pathSegments.Length == 1)
        {
            // Top-level children collection on the column: upsert by identifier so repeated
            // ChildAdded events for the same identifier (e.g. during replay) update in place
            // instead of duplicating. Mirrors how MongoDB's projection engine treats a child
            // re-introduced via ChildAdded as a replacement when the identifier already exists.
            var list = current as IList<object?> ?? new List<object?>();
            UpsertChildInList(list, identifierName, identifierValue, unwrappedState);
            entry.Entity[column.Name] = SerializeJsonColumn(list, column);
        }
        else
        {
            // Nested children — e.g. Configurations.Hubs — locate the matching parent inside the
            // column's JSON tree using ArrayIndexers, then upsert into its child collection.
            var rootList = current as IList<object?> ?? new List<object?>();
            var indexers = childAdded.ArrayIndexers ?? ArrayIndexers.NoIndexers;
            UpsertNestedChild(rootList, pathSegments, 1, indexers, identifierName, identifierValue, unwrappedState);
            entry.Entity[column.Name] = SerializeJsonColumn(rootList, column);
        }

        entry.Property(column.Name).IsModified = true;
    }

    void ApplyChildRemoved(EntityEntry<DynamicReadModelEntity> entry, ChildRemoved childRemoved)
    {
        var columnName = childRemoved.ChildrenProperty.Segments.First().Value;
        var column = _columns.FirstOrDefault(c => string.Equals(c.Name, columnName, StringComparison.Ordinal));
        if (column?.IsJson != true)
        {
            return;
        }

        if (LoadJsonColumnState(entry, column) is not IList<object?> current)
        {
            return;
        }

        var identifier = childRemoved.IdentifiedByProperty.LastSegment.Value;
        var keyAsString = childRemoved.Key?.ToString();
        var retained = new List<object?>(current.Count);
        foreach (var item in current)
        {
            if (item is IDictionary<string, object?> dict
                && dict.TryGetValue(identifier, out var value)
                && (Equals(value, childRemoved.Key) || (value is not null && value.ToString() == keyAsString)))
            {
                continue;
            }

            retained.Add(item);
        }

        entry.Entity[column.Name] = SerializeJsonColumn(retained, column);
        entry.Property(column.Name).IsModified = true;
    }

    void ApplyNestedCleared(EntityEntry<DynamicReadModelEntity> entry, NestedCleared nestedCleared)
    {
        var pathSegments = nestedCleared.NestedProperty.Segments.ToArray();
        if (pathSegments.Length == 0)
        {
            return;
        }

        var columnName = pathSegments[0].Value;
        var column = _columns.FirstOrDefault(c => string.Equals(c.Name, columnName, StringComparison.Ordinal));
        if (column is null)
        {
            return;
        }

        if (pathSegments.Length == 1)
        {
            // Clearing the column itself — represent "no value" as JSON null so that downstream
            // deserialization sees a missing object rather than an empty record. NestedCleared is
            // explicit about wiping the nested object; preserving a JSON "{}" would resurrect it
            // as an empty instance and break tests that assert ShouldBeNull.
            entry.Entity[column.Name] = column.IsJson ? "null" : null;
            entry.Property(column.Name).IsModified = true;
            return;
        }

        if (!column.IsJson)
        {
            return;
        }

        // Clear a sub-path within the JSON column — navigate to the parent, then null out the
        // final property. Mirrors the column-level case for nested-in-children scenarios.
        var current = LoadJsonColumnState(entry, column);
        var indexers = nestedCleared.ArrayIndexers ?? ArrayIndexers.NoIndexers;
        var subPath = BuildSubPath(nestedCleared.NestedProperty);
        if (!subPath.Segments.Any())
        {
            entry.Entity[column.Name] = column.IsArray ? "[]" : "null";
            entry.Property(column.Name).IsModified = true;
            return;
        }

        var rebuilt = ApplyDifferenceToJsonRoot(current, column, subPath, indexers, null);
        entry.Entity[column.Name] = SerializeJsonColumn(rebuilt, column);
        entry.Property(column.Name).IsModified = true;
    }

    async Task<EntityEntry<DynamicReadModelEntity>> GetOrAttachEntity(DbContextScope<ReadModelDbContext> scope, string id, Key key)
    {
        var existing = await scope.DbContext.Entries.FirstOrDefaultAsync(BuildIdPredicate(id));
        if (existing is not null)
        {
            return scope.DbContext.Entries.Entry(existing);
        }

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
        var dict = new Dictionary<string, object?>();
        foreach (var column in _columns)
        {
            if (!entity.TryGetValue(column.Name, out var value) || value is null)
            {
                continue;
            }

            if (column.IsJson && value is string jsonText)
            {
                dict[column.Name] = string.IsNullOrEmpty(jsonText) ? null : JsonNode.Parse(jsonText);
            }
            else
            {
                dict[column.Name] = JsonSerializer.SerializeToNode(value, ReadModelDbContext.JsonSerializerOptions);
            }
        }

        var json = (JsonObject)JsonSerializer.SerializeToNode(dict, ReadModelDbContext.JsonSerializerOptions)!;
        return _expandoObjectConverter.ToExpandoObject(json, _schema);
    }

    void SeedDefaults(DynamicReadModelEntity entity)
    {
        foreach (var column in _columns)
        {
            if (column.IsKey || column.IsNullable)
            {
                continue;
            }

            if (column.IsJson)
            {
                entity[column.Name] = column.IsArray ? "[]" : "{}";
            }
            else if (column.ClrType == typeof(string))
            {
                entity[column.Name] = string.Empty;
            }
            else if (column.ClrType.IsValueType)
            {
                entity[column.Name] = Activator.CreateInstance(column.ClrType);
            }
        }
    }

    void SetTypedColumn(EntityEntry<DynamicReadModelEntity> entry, ProjectedColumn column, object? value)
    {
        entry.Entity[column.Name] = CoerceForColumn(column, value);
        entry.Property(column.Name).IsModified = true;
    }

    void AdvanceLastHandledSequenceNumber(EntityEntry<DynamicReadModelEntity> entry, EventSequenceNumber eventSequenceNumber)
    {
        var column = _columns.FirstOrDefault(c => string.Equals(c.Name, WellKnownProperties.LastHandledEventSequenceNumber, StringComparison.Ordinal));
        if (column is null)
        {
            return;
        }

        var incoming = (long)(ulong)eventSequenceNumber;
        var existing = entry.Entity.TryGetValue(column.Name, out var existingValue) && existingValue is not null
            ? Convert.ToInt64(existingValue, CultureInfo.InvariantCulture)
            : 0L;
        var next = Math.Max(incoming, existing);
        if (next == existing && entry.State != EntityState.Added)
        {
            return;
        }

        entry.Entity[column.Name] = next;
        entry.Property(column.Name).IsModified = true;
    }

    object? LoadJsonColumnState(EntityEntry<DynamicReadModelEntity> entry, ProjectedColumn column)
    {
        if (!entry.Entity.TryGetValue(column.Name, out var current) || current is null)
        {
            return column.IsArray ? new List<object?>() : new ExpandoObject();
        }

        if (current is string jsonString)
        {
            return DeserializeJsonColumn(jsonString) ?? (column.IsArray ? new List<object?>() : new ExpandoObject());
        }

        return current;
    }

    object ApplyDifferenceToJsonRoot(object? current, ProjectedColumn column, PropertyPath subPath, ArrayIndexers indexers, object? changed)
    {
        if (current is ExpandoObject rootExpando)
        {
            subPath.SetValue(rootExpando, changed!, indexers);
            return rootExpando;
        }

        if (current is IList<object?> list)
        {
            // The JSON column is an array (e.g. Features). The change targets a specific element
            // selected by an ArrayIndexer whose ArrayProperty matches the column name; the subPath
            // is the property path WITHIN that element. EnsurePath cannot run here because it
            // navigates from an ExpandoObject root and would mis-classify the column name as a
            // PropertyName instead of an ArrayProperty — instead, locate the element ourselves
            // and apply the inner change directly to it. Sub-element indexers (deeper arrays inside
            // the located element, e.g. Slices inside Features) need their ArrayProperty rewritten
            // to drop the column prefix so EnsurePath can find them while walking the subPath.
            var indexer = indexers.All.FirstOrDefault(i => i.ArrayProperty.LastSegment.Value == column.Name);
            if (indexer is null)
            {
                return list;
            }

            var element = LocateOrCreateChild(list, indexer);
            if (!subPath.Segments.Any())
            {
                // The whole element is being replaced.
                ReplaceChildContents(element, changed);
            }
            else
            {
                var rewrittenIndexers = RewriteIndexersRelativeTo(column.Name, indexers);
                subPath.SetValue(element, changed!, rewrittenIndexers);
            }

            return list;
        }

        var fresh = new ExpandoObject();
        subPath.SetValue(fresh, changed!, indexers);
        return fresh;
    }

    static ArrayIndexers RewriteIndexersRelativeTo(string columnName, ArrayIndexers indexers)
    {
        // The change's indexers carry absolute paths from the read model root (e.g.
        // "Features.Slices"). After we have descended into the column's array element we hand
        // EnsurePath a sub-context, and it walks paths relative to that element. Drop the column
        // prefix from every indexer's ArrayProperty / IdentifierProperty so they match the path
        // segments EnsurePath sees while traversing the subPath.
        var rewritten = new List<ArrayIndexer>();
        foreach (var indexer in indexers.All)
        {
            var arraySegments = indexer.ArrayProperty.Segments.ToArray();
            if (arraySegments.Length == 0 || !string.Equals(arraySegments[0].Value, columnName, StringComparison.Ordinal))
            {
                rewritten.Add(indexer);
                continue;
            }

            if (arraySegments.Length == 1)
            {
                // The indexer was for the column itself — no longer needed inside its element.
                continue;
            }

            var trimmedArray = PropertyPath.CreateFrom(arraySegments.Skip(1).ToArray());
            var identifierSegments = indexer.IdentifierProperty.Segments.ToArray();
            var trimmedIdentifier = identifierSegments.Length > 0 && string.Equals(identifierSegments[0].Value, columnName, StringComparison.Ordinal)
                ? PropertyPath.CreateFrom(identifierSegments.Skip(1).ToArray())
                : indexer.IdentifierProperty;

            // Normalize identifiers to string form. JSON round-trips turn Guid identifiers into
            // strings and strip ConceptAs<T> wrappers, so the inside-EnsurePath comparison of the
            // stored value (string) against the original Identifier (concept/Guid) never matches
            // and manufactures a duplicate entry instead of finding the existing one. Comparing
            // the two as strings makes the lookup deterministic across types.
            var normalizedIdentifier = UnwrapConceptValue(indexer.Identifier)?.ToString();
            rewritten.Add(new ArrayIndexer(trimmedArray, trimmedIdentifier, normalizedIdentifier!));
        }

        return new ArrayIndexers(rewritten);
    }

    static ExpandoObject LocateOrCreateChild(IList<object?> list, ArrayIndexer indexer)
    {
        var identifierName = indexer.IdentifierProperty.LastSegment.Value;
        var identifierValue = UnwrapConceptValue(indexer.Identifier);
        var identifierAsString = identifierValue?.ToString();

        for (var i = 0; i < list.Count; i++)
        {
            if (list[i] is not IDictionary<string, object?> dict)
            {
                continue;
            }

            if (!dict.TryGetValue(identifierName, out var stored))
            {
                continue;
            }

            var storedAsString = stored?.ToString();
            if (Equals(stored, identifierValue) || string.Equals(storedAsString, identifierAsString, StringComparison.Ordinal))
            {
                if (list[i] is ExpandoObject existing)
                {
                    return existing;
                }

                // Reify the dictionary as an ExpandoObject so EnsurePath can navigate it.
                var promoted = new ExpandoObject();
                var promotedDict = (IDictionary<string, object?>)promoted;
                foreach (var kvp in dict)
                {
                    promotedDict[kvp.Key] = kvp.Value;
                }

                list[i] = promoted;
                return promoted;
            }
        }

        var fresh = new ExpandoObject();
        ((IDictionary<string, object?>)fresh)[identifierName] = identifierValue;
        list.Add(fresh);
        return fresh;
    }

    static void ReplaceChildContents(ExpandoObject target, object? replacement)
    {
        var dict = (IDictionary<string, object?>)target;
        dict.Clear();
        if (replacement is ExpandoObject sourceExpando)
        {
            foreach (var kvp in (IDictionary<string, object?>)sourceExpando)
            {
                dict[kvp.Key] = kvp.Value;
            }
        }
        else if (replacement is IDictionary<string, object?> sourceDict)
        {
            foreach (var kvp in sourceDict)
            {
                dict[kvp.Key] = kvp.Value;
            }
        }
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

    static object? CoerceForColumn(ProjectedColumn column, object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value.IsConcept())
        {
            value = value.GetConceptValue();
        }

        var clr = Nullable.GetUnderlyingType(column.ClrType) ?? column.ClrType;
        if (clr.IsInstanceOfType(value))
        {
            return value;
        }

        try
        {
            if (clr.IsEnum)
            {
                return Convert.ChangeType(value, Enum.GetUnderlyingType(clr), CultureInfo.InvariantCulture);
            }

            if (clr == typeof(Guid))
            {
                return value is string s ? Guid.Parse(s) : value;
            }

            if (clr == typeof(DateTime) && value is string dt)
            {
                return DateTime.Parse(dt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            }

            if (clr == typeof(DateTimeOffset) && value is string dto)
            {
                return DateTimeOffset.Parse(dto, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            }

            if (clr == typeof(DateOnly) && value is string d)
            {
                return DateOnly.Parse(d, CultureInfo.InvariantCulture);
            }

            if (clr == typeof(TimeOnly) && value is string t)
            {
                return TimeOnly.Parse(t, CultureInfo.InvariantCulture);
            }

            return Convert.ChangeType(value, clr, CultureInfo.InvariantCulture);
        }
        catch
        {
            return value;
        }
    }

    static object? DeserializeJsonColumn(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(json);
        return ConvertJsonElement(doc.RootElement);
    }

    static object? ConvertJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var expando = new ExpandoObject();
                var dict = (IDictionary<string, object?>)expando;
                foreach (var property in element.EnumerateObject())
                {
                    dict[property.Name] = ConvertJsonElement(property.Value);
                }

                return expando;

            case JsonValueKind.Array:
                var list = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(ConvertJsonElement(item));
                }

                return list;

            case JsonValueKind.String:
                return element.GetString();

            case JsonValueKind.Number:
                if (element.TryGetInt64(out var i))
                {
                    return i;
                }

                return element.GetDouble();

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            default:
                return null;
        }
    }

    static string SerializeJsonColumn(object? value, ProjectedColumn column)
    {
        if (value is null)
        {
            return column.IsArray ? "[]" : "{}";
        }

        var unwrapped = UnwrapForJson(value);
        return JsonSerializer.Serialize(unwrapped, ReadModelDbContext.JsonSerializerOptions);
    }

    static object? UnwrapForJson(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value.IsConcept())
        {
            return value.GetConceptValue();
        }

        if (value is ExpandoObject expando)
        {
            var dict = new Dictionary<string, object?>();
            foreach (var kvp in (IDictionary<string, object?>)expando)
            {
                dict[kvp.Key] = UnwrapForJson(kvp.Value);
            }

            return dict;
        }

        if (value is IDictionary<string, object?> dictionary)
        {
            var result = new Dictionary<string, object?>();
            foreach (var kvp in dictionary)
            {
                result[kvp.Key] = UnwrapForJson(kvp.Value);
            }

            return result;
        }

        if (value is string)
        {
            return value;
        }

        if (value is IEnumerable enumerable)
        {
            var list = new List<object?>();
            foreach (var item in enumerable)
            {
                list.Add(UnwrapForJson(item));
            }

            return list;
        }

        return value;
    }

    static PropertyPath BuildSubPath(PropertyPath path)
    {
        var segments = path.Segments.Skip(1).ToArray();
        if (segments.Length == 0)
        {
            return new PropertyPath(string.Empty);
        }

        return PropertyPath.CreateFrom(segments);
    }

    static bool TryFindValueInDeserialized(object? node, IPropertyPathSegment[] segments, int index, object target)
    {
        if (node is null || index >= segments.Length)
        {
            return false;
        }

        var segment = segments[index];
        switch (node)
        {
            case IList<object?> list:
                foreach (var item in list)
                {
                    if (TryFindValueInDeserialized(item, segments, index, target))
                    {
                        return true;
                    }
                }

                return false;

            case IDictionary<string, object?> dict:
                if (!dict.TryGetValue(segment.Value, out var value))
                {
                    return false;
                }

                if (index == segments.Length - 1)
                {
                    return CompareLoose(value, target);
                }

                return TryFindValueInDeserialized(value, segments, index + 1, target);

            default:
                if (index == segments.Length - 1)
                {
                    return CompareLoose(node, target);
                }

                return false;
        }
    }

    static bool CompareLoose(object? value, object? target)
    {
        if (value is null && target is null)
        {
            return true;
        }

        if (value is null || target is null)
        {
            return false;
        }

        if (value.Equals(target))
        {
            return true;
        }

        if (target.IsConcept() && value.Equals(target.GetConceptValue()))
        {
            return true;
        }

        if (value.IsConcept() && (value.GetConceptValue()?.Equals(target) ?? false))
        {
            return true;
        }

        return string.Equals(value.ToString(), target.ToString(), StringComparison.Ordinal);
    }

    static (IPropertyPathSegment[] Segments, object? Value) GetChildJoinFilter(Joined joined)
    {
        var lastIndexer = joined.ArrayIndexers.All.Last();
        var combined = lastIndexer.ArrayProperty + lastIndexer.IdentifierProperty;
        return (combined.Segments.ToArray(), lastIndexer.Identifier);
    }

    static void UpsertChildInList(IList<object?> list, string identifierName, object? identifierValue, object? newChild)
    {
        // Upsert by identifier so repeated ChildAdded events (e.g. during replay, or when a
        // projection re-introduces the same child after a redaction) update in place instead of
        // appending duplicates. Merge into the existing entry so that any child collections
        // populated by earlier ChildAdded events (e.g. Slices.Events) survive the replacement of
        // the parent's own properties — a wholesale replace would clobber them.
        var identifierAsString = identifierValue?.ToString();
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i] is IDictionary<string, object?> existing
                && existing.TryGetValue(identifierName, out var stored)
                && (Equals(stored, identifierValue) || (stored?.ToString() == identifierAsString)))
            {
                if (newChild is IDictionary<string, object?> incoming)
                {
                    foreach (var kvp in incoming)
                    {
                        existing[kvp.Key] = kvp.Value;
                    }
                }
                else
                {
                    list[i] = AsExpando(newChild);
                }

                return;
            }
        }

        list.Add(AsExpando(newChild));
    }

    static object? AsExpando(object? value)
    {
        // EnsurePath walks ExpandoObject roots and filters list items via OfType<ExpandoObject>();
        // any dictionary in the list that isn't an actual ExpandoObject becomes invisible to the
        // path walker, which then creates a *second* element at the same identifier instead of
        // mutating the existing one. Promote Dictionary<string, object?> values (the form
        // UnwrapForJson produces from ExpandoObject) to ExpandoObject before they enter the list.
        if (value is ExpandoObject)
        {
            return value;
        }

        if (value is IDictionary<string, object?> dict)
        {
            var expando = new ExpandoObject();
            var expandoDict = (IDictionary<string, object?>)expando;
            foreach (var kvp in dict)
            {
                expandoDict[kvp.Key] = kvp.Value;
            }

            return expando;
        }

        return value;
    }

    static void UpsertNestedChild(IList<object?> parentList, IPropertyPathSegment[] segments, int index, ArrayIndexers indexers, string identifierName, object? identifierValue, object? newChild)
    {
        var parentSegmentName = segments[index - 1].Value;
        var indexer = indexers.All.FirstOrDefault(i => i.ArrayProperty.LastSegment.Value == parentSegmentName);
        if (indexer is null)
        {
            return;
        }

        var parentIdentifierName = indexer.IdentifierProperty.LastSegment.Value;
        var parentIdentifierValue = UnwrapConceptValue(indexer.Identifier);
        var parentIdentifierAsString = parentIdentifierValue?.ToString();

        foreach (var item in parentList)
        {
            if (item is not IDictionary<string, object?> parentDict)
            {
                continue;
            }

            if (!parentDict.TryGetValue(parentIdentifierName, out var matchValue))
            {
                continue;
            }

            var matchAsString = matchValue?.ToString();
            if (!Equals(matchValue, parentIdentifierValue) && !string.Equals(matchAsString, parentIdentifierAsString, StringComparison.Ordinal))
            {
                continue;
            }

            var childCollectionName = segments[index].Value;
            IList<object?> childList;
            if (parentDict.TryGetValue(childCollectionName, out var existing) && existing is IList<object?> existingList)
            {
                childList = existingList;
            }
            else
            {
                childList = new List<object?>();
                parentDict[childCollectionName] = childList;
            }

            if (index == segments.Length - 1)
            {
                UpsertChildInList(childList, identifierName, identifierValue, newChild);
                return;
            }

            UpsertNestedChild(childList, segments, index + 1, indexers, identifierName, identifierValue, newChild);
            return;
        }
    }

    static object? UnwrapConceptValue(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value.IsConcept())
        {
            return value.GetConceptValue();
        }

        return value;
    }

    static bool IsUniqueViolation(DbUpdateException ex)
    {
        // SqliteException error code 19 is SQLITE_CONSTRAINT; the message names UNIQUE / PRIMARY KEY.
        // PostgreSQL's PostgresException sets SqlState 23505; SQL Server's SqlException sets Number 2627/2601.
        // We treat them all alike: the row already exists, the caller should retry as UPDATE.
        var inner = ex.InnerException?.GetType().FullName ?? string.Empty;
        var message = ex.InnerException?.Message ?? string.Empty;
        if (inner.EndsWith("SqliteException", StringComparison.Ordinal) && message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (inner.EndsWith("PostgresException", StringComparison.Ordinal) && message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (inner.EndsWith("SqlException", StringComparison.Ordinal) && (message.Contains("PRIMARY KEY", StringComparison.OrdinalIgnoreCase) || message.Contains("UNIQUE KEY", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    static string GetIdString(Key key)
    {
        if (key.Value is ExpandoObject expandoKey)
        {
            var parts = expandoKey
                .OrderBy(kv => kv.Key)
                .Select(kv => kv.Value?.ToString() ?? string.Empty);
            return string.Join('_', parts);
        }

        return key.Value?.ToString() ?? string.Empty;
    }

    sealed class ObservableInstancesDisposable(IDisposable subscription, IAsyncDisposable scope) : IDisposable
    {
        bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            subscription.Dispose();
            scope.DisposeAsync().AsTask().Wait();
            _disposed = true;
        }
    }
}
