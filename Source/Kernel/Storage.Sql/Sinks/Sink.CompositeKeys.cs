// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// The composite key handling of the sink: the canonical id and the compatibility with ids stored by the legacy underscore join.
/// </summary>
public partial class Sink
{
    /// <summary>
    /// The length of the key column, which must match the one the ReadModelMigrator creates.
    /// </summary>
    const int KeyColumnLength = 200;

    bool _legacyReplayRecommended;
    string? _tableWithoutLegacyRows;

    static bool IsComposite(Key key) => key.Value is ExpandoObject;

    static bool IsAmbiguousComposite(Key key) => IsComposite(key) && GetLegacyIdString(key) is null;

    /// <summary>
    /// Gets the id the legacy sink stored the key under, when that id could only ever have been produced by this key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The legacy id, or null when the key is not composite or is ambiguous in that format.</returns>
    static string? GetLegacyIdString(Key key)
    {
        if (key.Value is not ExpandoObject composite)
        {
            return null;
        }

        var parts = composite
            .OrderBy(part => part.Key)
            .Select(part => part.Value?.ToString())
            .ToArray();

        return parts.Any(part => string.IsNullOrEmpty(part) || part.Contains('_')) ? null : string.Join('_', parts);
    }

    static CompositeKeyPartKind KindOf(JsonObjectType type) => type switch
    {
        _ when type.HasFlag(JsonObjectType.Integer) => CompositeKeyPartKind.WholeNumber,
        _ when type.HasFlag(JsonObjectType.Number) => CompositeKeyPartKind.Number,
        _ when type.HasFlag(JsonObjectType.Boolean) => CompositeKeyPartKind.Boolean,
        _ => CompositeKeyPartKind.Text
    };

    /// <summary>
    /// Refuses a read that cannot be answered truthfully: an ambiguous composite key that missed, while the read model still has legacy rows.
    /// </summary>
    /// <param name="scope">The scope to look for legacy rows in.</param>
    /// <param name="key">The <see cref="Key"/> that was not found.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="LegacyCompositeKeyRequiresReplay">The key is ambiguous in the legacy format and the read model still has legacy rows.</exception>
    async Task RefuseWhenLegacyRowsMayHoldTheKey(DbContextScope<ReadModelDbContext> scope, Key key)
    {
        if (!IsAmbiguousComposite(key) || !await HasLegacyRows(scope))
        {
            return;
        }

        await RecommendReplayOnce();
        throw new LegacyCompositeKeyRequiresReplay(_readModelIdentifier, GetIdString(key));
    }

    string GetIdString(Key key)
    {
        if (key.Value is not ExpandoObject composite)
        {
            return key.Value?.ToString() ?? string.Empty;
        }

        var id = CompositeKeyEncoding.Encode(composite);
        if (id.Length > KeyColumnLength)
        {
            throw new CompositeKeyTooLong(_readModelIdentifier, id.Length, KeyColumnLength);
        }

        return id;
    }

    async Task<string> ResolveStoredId(DbContextScope<ReadModelDbContext> scope, Key key)
    {
        var id = GetIdString(key);
        if (GetLegacyIdString(key) is not { } legacyId || await scope.DbContext.Entries.AnyAsync(BuildIdPredicate(id)))
        {
            return id;
        }

        return await scope.DbContext.Entries.AnyAsync(BuildIdPredicate(legacyId)) ? legacyId : id;
    }

    async Task<bool> HasLegacyRows(DbContextScope<ReadModelDbContext> scope)
    {
        var keyColumn = _columns.FirstOrDefault(c => c.IsKey);
        if (keyColumn is null || keyColumn.ClrType != typeof(string))
        {
            return false;
        }

        var table = ActiveTableName;
        if (_tableWithoutLegacyRows == table)
        {
            return false;
        }

        var name = keyColumn.Name;
        const string prefix = CompositeKeyEncoding.Prefix;
        var hasLegacyRows = await scope.DbContext.Entries.AnyAsync(e => !((string)e[name]!).StartsWith(prefix));
        if (!hasLegacyRows)
        {
            _tableWithoutLegacyRows = table;
        }

        return hasLegacyRows;
    }

    /// <summary>
    /// Moves the row stored under the legacy id of the key to the canonical id, within the changes of the scope.
    /// </summary>
    /// <param name="scope">The scope holding the changes.</param>
    /// <param name="id">The canonical id.</param>
    /// <param name="key">The key.</param>
    /// <returns>The entry of the renamed row, or null when there is no legacy row for the key.</returns>
    async Task<EntityEntry<DynamicReadModelEntity>?> RenameLegacyEntity(DbContextScope<ReadModelDbContext> scope, string id, Key key)
    {
        if (GetLegacyIdString(key) is not { } legacyId)
        {
            return null;
        }

        var legacy = await scope.DbContext.Entries.FirstOrDefaultAsync(BuildIdPredicate(legacyId));
        if (legacy is null)
        {
            return null;
        }

        var renamed = new DynamicReadModelEntity();
        foreach (var (name, value) in legacy)
        {
            renamed[name] = value;
        }

        renamed[_columns.First(c => c.IsKey).Name] = id;
        scope.DbContext.Entries.Remove(legacy);
        scope.DbContext.Entries.Add(renamed);
        return scope.DbContext.Entries.Entry(renamed);
    }

    async Task RecommendReplayForLegacyRows(DbContextScope<ReadModelDbContext> scope, Key key)
    {
        if (_legacyReplayRecommended || !IsAmbiguousComposite(key) || !await HasLegacyRows(scope))
        {
            return;
        }

        await RecommendReplayOnce();
    }

    async Task RecommendReplayOnce()
    {
        if (_legacyReplayRecommended || _compositeKeyReplayRecommender is null)
        {
            return;
        }

        _legacyReplayRecommended = true;
        await _compositeKeyReplayRecommender.Recommend(_eventStoreName, _namespace, _readModel);
    }

    Dictionary<string, CompositeKeyPartKind> GetKeyPartKinds(ProjectedColumn column) =>
        _schema.Properties.TryGetValue(column.Name, out var property)
            ? property.ActualSchema.Properties.ToDictionary(part => part.Key, part => KindOf(part.Value.ActualSchema.Type))
            : new Dictionary<string, CompositeKeyPartKind>();
}
