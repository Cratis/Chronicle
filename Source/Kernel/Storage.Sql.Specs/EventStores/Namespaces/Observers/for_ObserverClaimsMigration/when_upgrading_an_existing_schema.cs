// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverClaimsMigration;

public class when_upgrading_an_existing_schema : Specification
{
    IReadOnlyList<MigrationOperation> _operations;
    Dictionary<string, AddColumnOperation> _columns;

    void Because()
    {
        _operations = new Migrations.v19_113_0 { ActiveProvider = "Microsoft.EntityFrameworkCore.Sqlite" }.UpOperations;
        _columns = _operations.OfType<AddColumnOperation>().ToDictionary(column => column.Name);
    }

    [Fact] void should_only_add_columns() => _operations.All(operation => operation is AddColumnOperation).ShouldBeTrue();
    [Fact] void should_add_only_the_three_shadow_columns() => _columns.Keys.ShouldContainOnly("PartitionClaims", "ObserverWideClaim", "OwnershipRevision");
    [Fact] void should_target_the_existing_observers_table() => _columns.Values.All(column => column.Table == WellKnownTableNames.Observers).ShouldBeTrue();
    [Fact] void should_allow_legacy_rows_to_have_no_claims() => _columns.Values.All(column => column.IsNullable).ShouldBeTrue();
    [Fact] void should_not_backfill_legacy_rows() => _columns.Values.All(column => column.DefaultValue is null && column.DefaultValueSql is null).ShouldBeTrue();
}
