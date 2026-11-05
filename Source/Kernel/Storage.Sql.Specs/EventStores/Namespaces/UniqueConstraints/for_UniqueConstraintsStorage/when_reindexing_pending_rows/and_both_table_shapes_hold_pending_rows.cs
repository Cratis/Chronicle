// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.when_reindexing_pending_rows;

[Collection(SqlServerCollection.Name)]
public class and_both_table_shapes_hold_pending_rows(SqlServerFixture fixture) : given.a_server_claims_storage(fixture)
{
    long _sourcePendingBefore;
    long _valuesPendingBefore;
    long _sourcePending;
    long _valuesPending;
    long _sourceExact;
    long _valuesExact;
    long _sourceRows;
    long _valuesRows;

    async Task Establish()
    {
        await Prepare(false);
        await CreateLegacySourceTable(_sourceTable);
        await CreateLegacyValuesTable(_valuesTable);
        await AddLegacyClaim(_sourceTable, "Alice", "one");
        await AddLegacyClaim(_sourceTable, "Bob", "two");
        await AddLegacyClaim(_valuesTable, "one", "Alice");
        await AddLegacyClaim(_valuesTable, "two", "Bob");
        await EnsureTables();
    }

    async Task Because()
    {
        _sourcePendingBefore = await Pending(_sourceTable);
        _valuesPendingBefore = await Pending(_valuesTable);

        // What the constraint reindex does: clear the scope, then save every claim again.
        await _storage.ClearValues(_perSource);
        await _storage.ClearValues(_perValue);
        await _storage.Save("Alice", _perSource, 1UL, "one");
        await _storage.Save("alice", _perSource, 2UL, "two");
        await _storage.Save("Alice", _perValue, 1UL, "one");
        await _storage.Save("alice", _perValue, 2UL, "two");

        _sourcePending = await Pending(_sourceTable);
        _valuesPending = await Pending(_valuesTable);
        _sourceExact = await Exact(_sourceTable);
        _valuesExact = await Exact(_valuesTable);
        _sourceRows = await Rows(_sourceTable);
        _valuesRows = await Rows(_valuesTable);
    }

    [Fact] void should_start_with_pending_rows_in_the_per_source_table() => _sourcePendingBefore.ShouldEqual(2);
    [Fact] void should_start_with_pending_rows_in_the_per_value_table() => _valuesPendingBefore.ShouldEqual(2);
    [Fact] void should_leave_no_pending_row_in_the_per_source_table() => _sourcePending.ShouldEqual(0);
    [Fact] void should_leave_no_pending_row_in_the_per_value_table() => _valuesPending.ShouldEqual(0);
    [Fact] void should_leave_only_exact_rows_in_the_per_source_table() => (_sourceExact == _sourceRows && _sourceRows == 2).ShouldBeTrue();
    [Fact] void should_leave_only_exact_rows_in_the_per_value_table() => (_valuesExact == _valuesRows && _valuesRows == 2).ShouldBeTrue();
}
