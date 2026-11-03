// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Migrations.for_v19_116_1.when_creating_the_reservations_table;

public class and_the_provider_is_sql_server : Specification
{
    CreateTableOperation _table;

    void Because() => _table = new v19_116_1 { ActiveProvider = "Microsoft.EntityFrameworkCore.SqlServer" }.UpOperations.OfType<CreateTableOperation>().Single();

    [Fact] void should_create_the_reservations_table() => _table.Name.ShouldEqual(WellKnownTableNames.ResourceNameReservations);
    [Fact] void should_use_the_expected_key_type() => _table.Columns.Single(column => column.Name == "Key").ColumnType!.ToUpperInvariant().ShouldEqual("NVARCHAR(450)");
    [Fact] void should_use_the_expected_owner_type() => _table.Columns.Single(column => column.Name == "Owner").ColumnType!.ToUpperInvariant().ShouldEqual("NVARCHAR(450)");
    [Fact] void should_compare_the_key_in_binary() => _table.Columns.Single(column => column.Name == "Key").Collation.ShouldEqual("Latin1_General_100_BIN2");
    [Fact] void should_compare_the_owner_in_binary() => _table.Columns.Single(column => column.Name == "Owner").Collation.ShouldEqual("Latin1_General_100_BIN2");
    [Fact] void should_not_allow_null_keys() => _table.Columns.Single(column => column.Name == "Key").IsNullable.ShouldBeFalse();
    [Fact] void should_not_allow_null_owners() => _table.Columns.Single(column => column.Name == "Owner").IsNullable.ShouldBeFalse();
    [Fact] void should_have_the_key_as_the_only_primary_key_column() => _table.PrimaryKey.Columns.ShouldEqual(["Key"]);
}
