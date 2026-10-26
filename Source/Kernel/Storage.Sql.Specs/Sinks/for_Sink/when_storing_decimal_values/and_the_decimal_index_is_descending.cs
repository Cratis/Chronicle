// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;

#pragma warning disable EF1002 // Constant identifiers and internal index names only; values are parameterized.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_storing_decimal_values;

public class and_the_decimal_index_is_descending : given.a_sqlite_ordered_decimal_index_sink
{
    string[] _indexes;
    string[] _amounts;
    string _plan;
    string _shadowIndex;
    string _oldName;

    protected override IndexDefinition Index => new("amount") { Properties = [new("amount", true)] };

    async Task Because()
    {
        await WriteAmounts("a");
        await using var context = OpenContext();
        _oldName = _indexPlan.Name(DatabaseType.Sqlite, Table);
        await context.Database.ExecuteSqlRawAsync($"CREATE INDEX \"{_oldName}\" ON {Table}(amount DESC)");
        await _sink.EnsureIndexes();
        await _sink.EnsureIndexes();
        _indexes = await Indexes(context, Table);
        _shadowIndex = _indexes.Single(name => name.StartsWith(ShadowColumnIndexes.Prefix, StringComparison.Ordinal));
        _amounts = await Amounts(context, "__order_amount IS NOT NULL", "__order_amount DESC");
        var plan = await context.Database.SqlQuery<QueryPlan>($"EXPLAIN QUERY PLAN SELECT id FROM {Table} ORDER BY __order_amount DESC").ToArrayAsync();
        _plan = string.Join(' ', plan.Select(row => row.Detail));
    }

    [Fact] void should_create_one_shadow_index() => _indexes.Count(name => name.StartsWith(ShadowColumnIndexes.Prefix, StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_remove_the_descending_text_index() => _indexes.ShouldNotContain(_oldName);
    [Fact] void should_order_numerically_descending() => _amounts.ShouldEqual(["100", "10", "2", "-10"]);
    [Fact] void should_use_the_shadow_index() => _plan.ShouldContain(_shadowIndex);
}
