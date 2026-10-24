// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

public partial class Sink
{
    /// <inheritdoc/>
    public async Task<CompositeKeyLookupResult> FindByCompositeKey(Key key)
    {
        var parts = (ExpandoObject)key.Value;
        var legacyId = GetIdString(key);
        var encoding = CompositeKeyEncoding.Encode(parts);
        await using var scope = await _database.ReadModelTable(_eventStoreName, _namespace, _tableName, _columns);
        var context = scope.DbContext;
        var sql = context.GetService<ISqlGenerationHelper>();
        var table = sql.DelimitIdentifier(context.TableName);
        var keyColumn = sql.DelimitIdentifier(context.Columns.Single(_ => _.IsKey).Name);
        var idAlias = sql.DelimitIdentifier(nameof(SqlCompositeKeyLookup.Candidate.Id));
        var stampAlias = sql.DelimitIdentifier(nameof(SqlCompositeKeyLookup.Candidate.Stamp));
        SqlCompositeKeyLookup.Candidate[] candidates;
#pragma warning disable EF1002 // Only delimited identifiers are interpolated. Values use SQL parameters.
        if (CanQueryCompositeKeyStamp(context, encoding))
        {
            var stamp = sql.DelimitIdentifier(SqlCompositeKeyShadow.ColumnName);
            candidates = await ShadowColumns.Access(context, SqlCompositeKeyShadow.ColumnName, () => context.Database.SqlQueryRaw<SqlCompositeKeyLookup.Candidate>(
                $"SELECT {keyColumn} AS {idAlias}, {stamp} AS {stampAlias} FROM {table} WHERE {stamp} = {{0}} OR {keyColumn} = {{1}}", encoding, legacyId).ToArrayAsync());
        }
        else
        {
            candidates = await context.Database.SqlQueryRaw<SqlCompositeKeyLookup.Candidate>(
                $"SELECT {keyColumn} AS {idAlias}, NULL AS {stampAlias} FROM {table} WHERE {keyColumn} = {{0}}", legacyId).ToArrayAsync();
        }

#pragma warning restore EF1002
        var decision = SqlCompositeKeyLookup.Decide(candidates, encoding, legacyId, CompositeKeyEncoding.IsLegacyIdAmbiguous(parts));
        if (decision.Outcome != CompositeKeyLookupOutcome.Found) return new(decision.Outcome, null);
        var entity = await context.Entries.AsNoTracking().FirstOrDefaultAsync(BuildIdPredicate(decision.IdToLoad!));
        // The row's key column holds the flattened or encoded id. The requested parts are the key that was
        // matched, so restore them as the key property instead of splitting the stored string.
        return entity is null ? CompositeKeyLookupResult.NotFound : CompositeKeyLookupResult.Found(Materialize(entity, rebuildCompositeKey: false, knownKey: parts));
    }

    static bool CanQueryCompositeKeyStamp(ReadModelDbContext context, string encoding) =>
        encoding.Length <= 850 &&
        context.Columns.Any(_ => _.IsCompositeKey) &&
        ShadowColumns.For(context).Any(_ => _.Name == SqlCompositeKeyShadow.ColumnName);
}
