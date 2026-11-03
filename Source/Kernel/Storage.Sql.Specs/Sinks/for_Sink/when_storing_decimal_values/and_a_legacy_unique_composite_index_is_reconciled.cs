// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts.ReadModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

#pragma warning disable EF1002 // Constant identifiers and internal index names only.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_storing_decimal_values;

public class and_a_legacy_unique_composite_index_is_reconciled : given.a_sqlite_ordered_decimal_index_sink
{
    string[] _indexes;
    string _oldName;

    protected override IndexDefinition Index => new("name") { Properties = [new("name"), new("amount", true)] };

    async Task Because()
    {
        await WriteAmounts("a");
        await using var context = OpenContext();
        _oldName = _indexPlan.PreviousName(DatabaseType.Sqlite, Table);
        await context.Database.ExecuteSqlRawAsync($"CREATE UNIQUE INDEX \"{_oldName}\" ON {Table}(name ASC, amount DESC); CREATE INDEX chronicle_idx_obsolete ON {Table}(name); CREATE INDEX manual_composite ON {Table}(name, amount)");
        var freshSink = (Sink)_harness.CreateSink(_definition);
        await freshSink.ReconcileIndexes();
        _indexes = await Indexes(context, Table);
    }

    [Fact] void should_keep_the_legacy_unique_index() => _indexes.ShouldContain(_oldName);
    [Fact] void should_create_the_decimal_order_index() => _indexes.ShouldContain(_indexPlan.Name(DatabaseType.Sqlite, Table));
    [Fact] void should_remove_undeclared_nonunique_indexes() => _indexes.ShouldNotContain("chronicle_idx_obsolete");
    [Fact] void should_keep_custom_indexes() => _indexes.ShouldContain("manual_composite");

    [Fact]
    async Task should_continue_enforcing_uniqueness()
    {
        await using var context = OpenContext();
        var exception = await Catch.Exception(async () => await context.Database.ExecuteSqlRawAsync($"INSERT INTO {Table}(id, name, amount) SELECT 'duplicate', name, amount FROM {Table} LIMIT 1"));
        exception.ShouldBeOfExactType<SqliteException>();
        ((SqliteException)exception).SqliteErrorCode.ShouldEqual(19);
    }
}
