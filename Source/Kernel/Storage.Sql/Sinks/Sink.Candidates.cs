// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using static Cratis.Chronicle.Storage.Sql.Sinks.JsonColumnDocuments;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

public partial class Sink
{
    async Task<List<DynamicReadModelEntity>> CandidateEntries(ReadModelDbContext context, ProjectedColumn column, object? value, bool tracking = true)
    {
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

        // CompareLoose also permits object/collection ToString matches. Those exotic targets
        // cannot safely be narrowed to JSON scalar nodes.
        if (unwrapped is null || unwrapped.ToString()?.StartsWith("System.", StringComparison.Ordinal) == true) return context.Entries;
        var helper = context.GetService<ISqlGenerationHelper>();
        var name = helper.DelimitIdentifier(column.Name);
        var table = helper.DelimitIdentifier(context.TableName);
        var key = _columns.FirstOrDefault(column => column.IsKey);
        if (key is null) return context.Entries;
        var id = helper.DelimitIdentifier(key.Name);
        var type = context.Database.GetDatabaseType();

        // Search every descendant, deliberately admitting numeric/boolean nodes regardless of
        // their text representation. The existing path walker remains the final authority;
        // this avoids false negatives from numeric widths, culture, concepts, and nested arrays.
        var sql = type switch
        {
            DatabaseType.Sqlite => $"SELECT t.* FROM {table} t WHERE EXISTS (SELECT 1 FROM json_tree(CASE WHEN json_valid(t.{name}) THEN t.{name} ELSE NULL END) j WHERE j.type IN ('integer','real','true','false') OR j.atom = {{0}} OR j.atom = {{1}})",
            DatabaseType.PostgreSql => $"SELECT t.* FROM {table} t WHERE jsonb_path_exists(t.{name}, '$.** ? (@.type() == \"number\" || @.type() == \"boolean\" || @ == $v || @ == $w)', jsonb_build_object('v', CAST({{0}} AS text), 'w', CAST({{1}} AS text)))",
            DatabaseType.SqlServer => $"WITH json_nodes AS (SELECT {id} AS row_id, CAST({name} AS nvarchar(max)) AS node, CAST(5 AS int) AS kind FROM {table} WHERE ISJSON({name})=1 UNION ALL SELECT n.row_id, j.value, CAST(j.type AS int) FROM json_nodes n CROSS APPLY OPENJSON(CASE WHEN n.kind IN (4,5) THEN n.node ELSE N'{{{{}}}}' END) j) SELECT t.* FROM {table} t WHERE EXISTS (SELECT 1 FROM json_nodes n WHERE n.row_id=t.{id} AND (n.kind IN (2,3) OR (n.kind=1 AND (n.node={{0}} OR n.node={{1}})))) OPTION (MAXRECURSION 0)",
            _ => null
        };
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
        // seconds, and SQLite decimal materialization rounds binary floating-point values.
        // Neither is safely narrowed by exact SQL equality.
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
