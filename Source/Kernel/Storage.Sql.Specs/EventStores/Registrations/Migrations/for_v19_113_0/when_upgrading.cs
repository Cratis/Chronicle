// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Registrations.Migrations.for_v19_113_0;

public class when_upgrading : Specification
{
    IReadOnlyList<MigrationOperation> _operations;
    CreateTableOperation _table;

    void Because()
    {
        _operations = new v19_113_0 { ActiveProvider = "Microsoft.EntityFrameworkCore.Sqlite" }.UpOperations;
        _table = _operations.OfType<CreateTableOperation>().Single();
    }

    [Fact] void should_create_the_event_store_registration_table() => _table.Name.ShouldEqual("ObserverRegistrations");
    [Fact] void should_use_a_unique_identity() => _table.PrimaryKey.Columns.ShouldEqual(["Id"]);
    [Fact] void should_include_compare_and_set_revision() => _table.Columns.Exists(column => column.Name == "Revision" && !column.IsNullable).ShouldBeTrue();
    [Fact] void should_include_the_complete_payload() => _table.Columns.Exists(column => column.Name == "Data" && !column.IsNullable).ShouldBeTrue();
    [Fact] void should_leave_existing_tables_untouched() => _operations.All(operation => operation is CreateTableOperation or CreateIndexOperation).ShouldBeTrue();
    [Fact] void should_not_initialize_existing_stores() => _operations.OfType<InsertDataOperation>().ShouldBeEmpty();
    [Fact] void should_index_the_owner_query() => _operations.OfType<CreateIndexOperation>().Any(index => index.Columns.Contains("Owner")).ShouldBeTrue();
    [Fact] void should_index_the_active_query() => _operations.OfType<CreateIndexOperation>().Any(index => index.Columns.Contains("IsActive")).ShouldBeTrue();
}
