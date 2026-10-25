// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.given;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_writing_composite_keys;

public class and_the_table_is_a_legacy_namespaced_table : a_composite_keyed_sql_read_model
{
    Exception? _error;
    DynamicReadModelEntity[] _rows;
    bool _migrationRequired;

    async Task Because()
    {
        var legacy = await ReopenAsLegacy();
        _error = await Catch.Exception(() => legacy.ApplyChanges(CompositeKey("a", "b"), ChangesetNamed("one"), 1UL));
        _rows = await _harness.ReadStoredRows();
        _migrationRequired = await ((ICanReportKeyFormatMigration)legacy).RequiresKeyFormatMigration();
    }

    [Fact] void should_refuse_the_write() => _error.ShouldBeOfExactType<ReadModelKeyFormatMigrationPending>();
    [Fact] void should_not_write_a_row() => _rows.Length.ShouldEqual(0);
    [Fact] void should_report_that_a_migration_is_required() => _migrationRequired.ShouldBeTrue();
}
