// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.given;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_writing_composite_keys;

public class and_the_table_is_new : a_composite_keyed_sql_read_model
{
    DynamicReadModelEntity[] _rows;
    bool _migrationRequired;

    async Task Because()
    {
        await _sink.ApplyChanges(CompositeKey("a_b", "c"), ChangesetNamed("one"), 1UL);
        await _sink.ApplyChanges(CompositeKey("a", "b_c"), ChangesetNamed("two"), 2UL);
        _rows = await _harness.ReadStoredRows();
        _migrationRequired = await ((Cratis.Chronicle.Storage.Sinks.ICanReportKeyFormatMigration)_sink).RequiresKeyFormatMigration();
    }

    [Fact] void should_store_both_keys_as_separate_rows() => _rows.Length.ShouldEqual(2);
    [Fact] void should_store_the_keys_in_the_current_format() => _rows.All(row => ((string)row["id"]!).StartsWith("~k1:", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_require_a_migration() => _migrationRequired.ShouldBeFalse();
}
