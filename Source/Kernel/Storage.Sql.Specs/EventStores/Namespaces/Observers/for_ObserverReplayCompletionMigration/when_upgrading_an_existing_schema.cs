// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverReplayCompletionMigration;

public class when_upgrading_an_existing_schema : Specification
{
    IReadOnlyList<MigrationOperation> _operations;
    AddColumnOperation _column;

    void Because()
    {
        _operations = new Migrations.v19_116_0_1 { ActiveProvider = "Microsoft.EntityFrameworkCore.Sqlite" }.UpOperations;
        _column = _operations.OfType<AddColumnOperation>().Single();
    }

    [Fact] void should_only_add_one_column() => _operations.Count.ShouldEqual(1);
    [Fact] void should_add_the_completion_intents() => _column.Name.ShouldEqual("PartitionReplayCompletions");
    [Fact] void should_target_the_existing_table() => _column.Table.ShouldEqual(WellKnownTableNames.Observers);
    [Fact] void should_allow_legacy_null_values() => _column.IsNullable.ShouldBeTrue();
    [Fact] void should_not_backfill_legacy_rows() => (_column.DefaultValue is null && _column.DefaultValueSql is null).ShouldBeTrue();
}
