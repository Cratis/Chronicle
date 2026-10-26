// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

#pragma warning disable EF1002 // Constant identifiers only.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_storing_decimal_values;

/// <summary>
/// Characterizes how SQLite orders json_extract of a nested JSON number today (issue #5352 premise): numerically, not as text.
/// </summary>
public class when_ordering_sqlite_nested_decimals : Specification
{
    SqlSinkHarness _harness;
    string[] _ordered;
    string[] _indexed;

    async Task Because()
    {
        _harness = new SqlSinkHarness();
        await using var context = new DbContext(new DbContextOptionsBuilder().UseSqlite(_harness.ConnectionString).Options);
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE nested(id TEXT PRIMARY KEY, details TEXT); INSERT INTO nested VALUES('a','{\"total\":100}'),('b','{\"total\":10}'),('c','{\"total\":2}'),('d','{\"total\":-10}'),('e','{\"total\":193.58}')");
        _ordered = await context.Database.SqlQueryRaw<string>("SELECT id AS Value FROM nested ORDER BY json_extract(details, '$.total') DESC").ToArrayAsync();
        await context.Database.ExecuteSqlRawAsync("CREATE INDEX nested_total ON nested(json_extract(details, '$.total') DESC)");
        _indexed = await context.Database.SqlQueryRaw<string>("SELECT id AS Value FROM nested INDEXED BY nested_total ORDER BY json_extract(details, '$.total') DESC").ToArrayAsync();
    }

    void Destroy() => _harness.Dispose();

    [Fact] void should_order_nested_numbers_numerically() => _ordered.ShouldEqual(["e", "a", "b", "c", "d"]);
    [Fact] void should_order_the_expression_index_numerically() => _indexed.ShouldEqual(["e", "a", "b", "c", "d"]);
}
