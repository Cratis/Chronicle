// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
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
    const string EncodedKeyPrefix = "{";

    bool _legacyReplayRecommended;

    /// <summary>
    /// Finds an instance by key for a reader that needs the true answer, refusing when the answer cannot be known.
    /// </summary>
    /// <param name="key">The <see cref="Key"/> to find.</param>
    /// <returns>The instance, or null if there is none.</returns>
    /// <exception cref="LegacyCompositeKeyRequiresReplay">The key is ambiguous in the legacy format and the read model still has legacy rows.</exception>
    public async Task<ExpandoObject?> FindForRead(Key key)
    {
        var found = await FindOrDefault(key);
        if (found is not null || !IsAmbiguousComposite(key))
        {
            return found;
        }

        await using var scope = await OpenActiveScope();
        if (await HasLegacyRows(scope))
        {
            throw new LegacyCompositeKeyRequiresReplay(_readModelIdentifier, GetIdString(key));
        }

        return null;
    }

    static string GetIdString(Key key) =>
        key.Value is ExpandoObject composite
            ? CompositeKeyEncoding.Encode(composite)
            : key.Value?.ToString() ?? string.Empty;

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

        var name = keyColumn.Name;
        return await scope.DbContext.Entries.AnyAsync(e => !((string)e[name]!).StartsWith(EncodedKeyPrefix));
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
        if (_legacyReplayRecommended || !IsAmbiguousComposite(key) || _compositeKeyReplayRecommender is null || !await HasLegacyRows(scope))
        {
            return;
        }

        _legacyReplayRecommended = true;
        await _compositeKeyReplayRecommender.Recommend(_eventStoreName, _namespace, _readModel);
    }
}
