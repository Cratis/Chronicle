// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;

#pragma warning disable EF1002 // Constant identifiers and internal index names only; values are parameterized.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_storing_decimal_values;

public class and_a_composite_index_contains_a_decimal : given.a_sqlite_ordered_decimal_index_sink
{
    string[] _indexes;
    string[] _amounts;
    string[] _range;
    string[] _promoted;
    string _newName;
    string _oldName;
    string _sql;
    string _plan;

    protected override IndexDefinition Index => new("name") { Properties = [new("name"), new("amount", true)] };

    async Task Because()
    {
        await WriteAmounts("a");
        await Write("b-1", "b", 5m);
        await using var context = OpenContext();
        _newName = _indexPlan.Name(DatabaseType.Sqlite, Table);
        _oldName = _indexPlan.PreviousName(DatabaseType.Sqlite, Table);
        await context.Database.ExecuteSqlRawAsync($"CREATE INDEX \"{_oldName}\" ON {Table}(name ASC, amount DESC); CREATE INDEX manual_composite ON {Table}(name, amount); CREATE UNIQUE INDEX \"chronicle_idx_unique_name\" ON {Table}(id)");
        await _sink.EnsureIndexes();
        await _sink.EnsureIndexes();
        _indexes = await Indexes(context, Table);
        _sql = await context.Database.SqlQuery<string>($"SELECT sql AS Value FROM sqlite_master WHERE name = {_newName}").SingleAsync();
        _amounts = await Amounts(context, "name = 'a'", "__order_amount DESC");
        _range = await Amounts(context, $"name = 'a' AND __order_amount >= '{SqliteDecimalSortKey.For(0m)}'", "__order_amount DESC");
        var plan = await context.Database.SqlQueryRaw<QueryPlan>($"EXPLAIN QUERY PLAN SELECT id FROM {Table} WHERE name = 'a' ORDER BY __order_amount DESC").ToArrayAsync();
        _plan = string.Join(' ', plan.Select(row => row.Detail));
        var replay = new ReplayContext(new ReadModelType("ordered-decimals", ReadModelGeneration.First), Table, $"{Table}_revert", DateTimeOffset.UtcNow);
        await _sink.BeginReplay(replay);
        await Write("replayed", "a", 9m);
        await _sink.EndReplay(replay);
        await _sink.EnsureIndexes();
        _promoted = await Indexes(context, Table);
    }

    [Fact] void should_key_the_index_by_the_order_column() => _sql.ShouldContain("__order_amount");
    [Fact] void should_not_key_the_index_by_the_text_column() => _sql.ShouldNotContain("\"amount\"");
    [Fact] void should_drop_the_old_text_composite() => _indexes.ShouldNotContain(_oldName);
    [Fact] void should_keep_custom_indexes() => _indexes.ShouldContain("manual_composite");
    [Fact] void should_keep_unique_indexes() => _indexes.ShouldContain("chronicle_idx_unique_name");
    [Fact] void should_order_numerically_descending() => _amounts.ShouldEqual(["100", "10", "2", "-10"]);
    [Fact] void should_range_scan_numerically() => _range.ShouldEqual(["100", "10", "2"]);
    [Fact] void should_use_the_composite_index() => _plan.ShouldContain(_newName);
    [Fact] void should_survive_a_replay() => _promoted.ShouldContain(_newName);
    [Fact] void should_not_resurrect_the_text_composite_after_a_replay() => _promoted.ShouldNotContain(_oldName);
}
