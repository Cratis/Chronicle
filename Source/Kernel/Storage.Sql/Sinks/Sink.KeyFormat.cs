// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Arc.Connections;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;

#pragma warning disable SA1202 // Private helpers grouped next to the methods they back.
#pragma warning disable SA1204 // Static helpers grouped next to the methods they back.

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// The part of <see cref="Sink"/> that decides how a composite key is encoded for the physical table being touched.
/// </summary>
public partial class Sink : ICanReportKeyFormatMigration
{
    /// <summary>
    /// The longest encoded composite key that fits the key column on every provider.
    /// </summary>
    internal const int MaxCompositeKeyLength = 450;

    ReadModelKeyFormat? _primaryFormat;
    ReadModelKeyFormat? _replayFormat;
    int _refusalLogged;
    int _legacyReadLogged;
    int _legacyIdInCurrentFormatLogged;
    int _legacyGlobalWriteLogged;

    bool IsGlobalScope => _namespace.Equals(Concepts.EventStoreNamespaceName.NotSet);

    /// <inheritdoc/>
    public async Task<bool> RequiresKeyFormatMigration()
    {
        if (!_compositeKey)
        {
            return false;
        }

        await using var scope = await _database.ReadModelTable(_eventStoreName, _namespace, _tableName, _columns);
        return await FormatOf(scope, _tableName) == ReadModelKeyFormat.Legacy;
    }

    static string LegacyIdFor(Key key)
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

    static string IdFor(Key key, ReadModelKeyFormat format) =>
        key.Value is ExpandoObject expandoKey && format != ReadModelKeyFormat.Legacy
            ? CompositeKeyEncoding.Encode(expandoKey)
            : LegacyIdFor(key);

    static bool IsStoredInCurrentFormat(ReadModelKeyFormat format) => format != ReadModelKeyFormat.Legacy;

    async Task<ReadModelKeyFormat> FormatOf(DbContextScope<ReadModelDbContext> scope, string table)
    {
        var cached = CachedFormatOf(table);
        if (cached is not null)
        {
            return cached.Value;
        }

        // Opening the scope migrates the table, which creates the format table first (ReadModelMigrator.EnsureTableMigrated).
        var id = ReadModelKeyFormatEntry.IdentifierFor(table);
        var stored = await scope.DbContext.ReadModelKeyFormats.AsNoTracking()
            .Where(entry => entry.Id == id)
            .Select(entry => entry.Format)
            .FirstOrDefaultAsync();
        var format = ReadModelKeyFormatEntry.FromStored(table, stored);

        // Only a current format is cached. It is terminal, whereas a legacy table can be promoted by another silo.
        if (IsStoredInCurrentFormat(format))
        {
            if (table == _tableName)
            {
                _primaryFormat = format;
            }
            else if (table == ReplayTableNameFor(_tableName))
            {
                _replayFormat = format;
            }
        }

        return format;
    }

    ReadModelKeyFormat? CachedFormatOf(string table)
    {
        if (table == _tableName)
        {
            return _primaryFormat;
        }

        return table == ReplayTableNameFor(_tableName) ? _replayFormat : null;
    }

    void ClearFormatCaches()
    {
        _primaryFormat = null;
        _replayFormat = null;
    }

    async Task<string> IdForRead(DbContextScope<ReadModelDbContext> scope, string table, Key key)
    {
        if (!_compositeKey)
        {
            return LegacyIdFor(key);
        }

        var format = await FormatOf(scope, table);
        if (_compositeKey && format == ReadModelKeyFormat.Legacy)
        {
            NoteLegacyRead(table);
        }

        return IdFor(key, format);
    }

    async Task<string> IdForWrite(DbContextScope<ReadModelDbContext> scope, string table, Key key)
    {
        if (!_compositeKey)
        {
            return LegacyIdFor(key);
        }

        var format = await FormatOf(scope, table);
        if (format == ReadModelKeyFormat.Legacy)
        {
            if (_compositeKey && table == _tableName)
            {
                GuardLegacyWrite(table);
            }

            return IdFor(key, format);
        }

        var id = IdFor(key, format);
        if (_compositeKey && id.Length > MaxCompositeKeyLength)
        {
            SqlStorageTelemetry.CompositeKeyWriteRefused("too-long");
            throw new CompositeKeyTooLong(table, id.Length);
        }

        return id;
    }

    void GuardLegacyWrite(string table)
    {
        if (IsGlobalScope)
        {
            // A namespaced replay cannot rebuild a global read model, so refusing would last forever.
            // Keep writing the legacy format until a coordinated global replay exists.
            SqlStorageTelemetry.CompositeKeyWriteLegacy("global");
            if (Interlocked.Exchange(ref _legacyGlobalWriteLogged, 1) == 0)
            {
                _logger.GlobalCompositeKeyWrittenInLegacyFormat(_eventStoreName.Value, table);
            }

            return;
        }

        SqlStorageTelemetry.CompositeKeyWriteRefused("legacy-format");
        if (Interlocked.Exchange(ref _refusalLogged, 1) == 0)
        {
            _logger.CompositeKeyWriteRefused(_eventStoreName.Value, _namespace.Value, table);
        }

        throw new ReadModelKeyFormatMigrationPending(table);
    }

    void NoteLegacyRead(string table)
    {
        SqlStorageTelemetry.LegacyCompositeKeyRead();
        if (Interlocked.Exchange(ref _legacyReadLogged, 1) == 0)
        {
            _logger.LegacyCompositeKeyRead(_eventStoreName.Value, _namespace.Value, table);
        }
    }

    async Task NoteKeyFormatOfInstances(DbContextScope<ReadModelDbContext> scope, string table, IEnumerable<DynamicReadModelEntity> entries)
    {
        if (!_compositeKey)
        {
            return;
        }

        var format = await FormatOf(scope, table);
        if (format == ReadModelKeyFormat.Legacy)
        {
            NoteLegacyRead(table);
            return;
        }

        var keyColumn = _columns.First(column => column.IsKey);
        if (!entries.Any(entry => !entry.TryGetValue(keyColumn.Name, out var value) || !CompositeKeyEncoding.IsEncoded(value?.ToString())))
        {
            return;
        }

        SqlStorageTelemetry.LegacyCompositeKeyInK1Table();
        if (Interlocked.Exchange(ref _legacyIdInCurrentFormatLogged, 1) == 0)
        {
            _logger.LegacyCompositeKeyInCurrentFormatTable(_eventStoreName.Value, _namespace.Value, table);
        }
    }

    async Task DropLegacyReplayTable()
    {
        if (!_compositeKey)
        {
            return;
        }

        // A replay table left by an older kernel holds legacy ids. Dropping it lets opening the scope recreate it in the current format.
        var replayName = ReplayTableNameFor(_tableName);
        await using var scope = await _database.ReadModelTable(_eventStoreName, _namespace, _tableName, _columns, ConnectionPurpose.Schema);
        var database = scope.DbContext.Database;
        if (!await TableExists(database, database.GetDatabaseType(), replayName))
        {
            return;
        }

        if (await FormatOf(scope, replayName) != ReadModelKeyFormat.Legacy)
        {
            return;
        }

        await ExecuteDdl(scope, BuildDropSql(scope, replayName));
        await DeleteKeyFormat(scope.DbContext, replayName);
    }

    static Task<int> DeleteKeyFormat(ReadModelDbContext context, string table)
    {
        var id = ReadModelKeyFormatEntry.IdentifierFor(table);
        return context.ReadModelKeyFormats.Where(entry => entry.Id == id).ExecuteDeleteAsync();
    }

    static async Task<string?> StoredKeyFormat(ReadModelDbContext context, string table)
    {
        var id = ReadModelKeyFormatEntry.IdentifierFor(table);
        return await context.ReadModelKeyFormats.AsNoTracking()
            .Where(entry => entry.Id == id)
            .Select(entry => entry.Format)
            .FirstOrDefaultAsync();
    }

    static async Task SetKeyFormat(ReadModelDbContext context, string table, ReadModelKeyFormat format)
    {
        await DeleteKeyFormat(context, table);
        if (format == ReadModelKeyFormat.Legacy)
        {
            return;
        }

        var entry = ReadModelKeyFormatEntry.For(table, format);
        context.ReadModelKeyFormats.Add(entry);
        try
        {
            await context.SaveChangesAsync();
        }
        finally
        {
            context.Entry(entry).State = EntityState.Detached;
        }
    }

    /// <summary>
    /// Moves the key format rows with the table renames of a promotion, inside the promotion transaction.
    /// </summary>
    /// <param name="context">The context sharing the promotion transaction.</param>
    /// <param name="promotion">The promotion being performed.</param>
    /// <param name="recoveryName">The name a markerless legacy backup is preserved under.</param>
    /// <param name="preservedLegacyBackup">Whether the revert table was preserved under the recovery name.</param>
    /// <returns>Awaitable task.</returns>
    async Task MoveKeyFormats(ReadModelDbContext context, ReplayTablePromotion promotion, string recoveryName, bool preservedLegacyBackup)
    {
        var replayFormat = ReadModelKeyFormatEntry.FromStored(promotion.ReplayName, await StoredKeyFormat(context, promotion.ReplayName));
        var primaryFormat = ReadModelKeyFormatEntry.FromStored(_tableName, await StoredKeyFormat(context, _tableName));
        var revertFormat = ReadModelKeyFormatEntry.FromStored(promotion.RevertName, await StoredKeyFormat(context, promotion.RevertName));

        if (preservedLegacyBackup)
        {
            await SetKeyFormat(context, recoveryName, revertFormat);
        }

        await SetKeyFormat(context, promotion.RevertName, primaryFormat);
        await SetKeyFormat(context, _tableName, replayFormat == ReadModelKeyFormat.Legacy ? ReadModelKeyFormat.Legacy : ReadModelKeyFormat.K1);
        await DeleteKeyFormat(context, promotion.ReplayName);
    }

    async Task<(bool IsLegacy, bool HasRows)> DescribeCompositePrimary()
    {
        if (!_compositeKey)
        {
            return (false, false);
        }

        await using var scope = await _database.ReadModelTable(_eventStoreName, _namespace, _tableName, _columns);
        var isLegacy = await FormatOf(scope, _tableName) == ReadModelKeyFormat.Legacy;
        return (isLegacy, isLegacy && await scope.DbContext.Entries.AsNoTracking().AnyAsync());
    }
}
