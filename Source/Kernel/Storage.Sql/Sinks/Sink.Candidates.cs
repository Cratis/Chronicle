// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using static Cratis.Chronicle.Storage.Sql.Sinks.JsonColumnCoercion;
using static Cratis.Chronicle.Storage.Sql.Sinks.JsonColumnDocuments;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

public partial class Sink
{
    static bool UsesDecimalSortKey(ReadModelDbContext context, ProjectedColumn column) =>
        context.Database.IsSqlite() && SqliteDecimalSortKey.IsDecimal(column);

    static Task<List<DynamicReadModelEntity>> DecimalCandidates(ReadModelDbContext context, ProjectedColumn column, decimal value, bool tracking)
    {
        var shadow = SqliteDecimalSortKey.ColumnNameFor(column.Name);
        var helper = context.GetService<ISqlGenerationHelper>();
        var sql = $"SELECT t.* FROM {helper.DelimitIdentifier(context.TableName)} t WHERE t.{helper.DelimitIdentifier(shadow)} = {{0}}";
        return ShadowColumns.Access(context, shadow, () =>
        {
#pragma warning disable EF1002 // All identifiers are provider-delimited and the sort key is parameterized.
            var query = context.Entries.FromSqlRaw(sql, SqliteDecimalSortKey.For(value));
#pragma warning restore EF1002
            return (tracking ? query : query.AsNoTracking()).ToListAsync();
        });
    }

    async Task<List<DynamicReadModelEntity>> CandidateEntries(ReadModelDbContext context, ProjectedColumn column, object? value, bool tracking = true)
    {
        if (UsesDecimalSortKey(context, column) && UnwrapConceptValue(value) is decimal number)
        {
            return await DecimalCandidates(context, column, number, tracking);
        }
        IQueryable<DynamicReadModelEntity> query = context.Entries;
        if (value is not null)
        {
            query = column.IsJson ? JsonCandidates(context, column, value) : ScalarCandidates(query, context.Database.GetDatabaseType(), column, value);
        }
        if (!tracking) query = query.AsNoTracking();
        return await query.ToListAsync();
    }

    IQueryable<DynamicReadModelEntity> JsonCandidates(ReadModelDbContext context, ProjectedColumn column, object value)
    {
        var unwrapped = UnwrapConceptValue(value);
        var composite = StructuralKeyEquality.IsStructured(unwrapped);
        if (composite)
        {
            var leaf = CompositeKeyCandidate.StringLeaf(unwrapped);
            if (leaf is null) return context.Entries;
            value = unwrapped = leaf;
        }

        // Keep broad handling when no safe scalar representation is available.
        if (ChildKeyComparison.RequiresJsonScan(unwrapped)) return context.Entries;
        var helper = context.GetService<ISqlGenerationHelper>();
        var name = helper.DelimitIdentifier(column.Name);
        var table = helper.DelimitIdentifier(context.TableName);
        var key = _columns.FirstOrDefault(column => column.IsKey);
        if (key is null) return context.Entries;
        var id = helper.DelimitIdentifier(key.Name);

        // Scalar searches still admit all numeric/boolean representations. Composite searches
        // constrain only an exact string leaf; the existing path walker remains the final authority.
        var sql = JsonCandidateSql.Create(context.Database.GetDatabaseType(), table, name, id, composite);
        if (sql is null) return context.Entries;
#pragma warning disable EF1002 // All identifiers are delimited; values remain SQL parameters.
        return context.Entries.FromSqlRaw(sql, value.ToString() ?? string.Empty, unwrapped.ToString() ?? string.Empty);
#pragma warning restore EF1002
    }

    IQueryable<DynamicReadModelEntity> ScalarCandidates(IQueryable<DynamicReadModelEntity> query, DatabaseType databaseType, ProjectedColumn column, object value)
    {
        var unwrapped = UnwrapConceptValue(value);
        if (unwrapped is null) return query;
        var type = Nullable.GetUnderlyingType(column.ClrType) ?? column.ClrType;

        // Loose comparison can match temporal ToString values with differing fractional
        // seconds. Other numeric representations of a SQLite decimal also remain advisory;
        // only an actual decimal (including an unwrapped concept) uses exact canonical narrowing.
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeOnly) ||
            type == typeof(TimeSpan) || (type == typeof(decimal) && databaseType == DatabaseType.Sqlite))
        {
            return query;
        }
        if (type != typeof(string) && type != unwrapped.GetType()) return query;
        if ((unwrapped is double number && !double.IsFinite(number)) || (unwrapped is float single && !float.IsFinite(single))) return query;
        object? coerced;
        try
        {
            coerced = type == typeof(string) ? unwrapped.ToString() : CoerceForColumn(column, unwrapped);
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException or ArgumentException)
        {
            return query;
        }
        if (coerced is null || !type.IsInstanceOfType(coerced)) return query;
        var entry = Expression.Parameter(typeof(DynamicReadModelEntity), "entry");
        var property = Expression.Call(typeof(EF), nameof(EF.Property), [column.ClrType], entry, Expression.Constant(column.Name));
        var constant = Expression.Convert(Expression.Constant(coerced), column.ClrType);
        Expression predicate = Expression.Equal(property, constant);
        if (type == typeof(string))
        {
            predicate = Expression.OrElse(predicate, Expression.Equal(property, Expression.Constant(value.ToString(), typeof(string))));
            predicate = Expression.OrElse(predicate, Expression.Equal(property, Expression.Constant(unwrapped.ToString(), typeof(string))));
        }
        return query.Where(Expression.Lambda<Func<DynamicReadModelEntity, bool>>(predicate, entry));
    }
}
