// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventStoreSubscriptions.Migrations.for_v19_160_0;

public class when_upgrading : Specification
{
    AddColumnOperation _column;
    void Because() => _column = new v19_160_0().UpOperations.OfType<AddColumnOperation>().Single();
    [Fact] void should_sort_after_subscription_table_creation() => string.CompareOrdinal(typeof(v19_160_0).GetCustomAttributes(false).OfType<MigrationAttribute>().Single().Id, typeof(v15_0_0).GetCustomAttributes(false).OfType<MigrationAttribute>().Single().Id).ShouldBeGreaterThan(0);
    [Fact] void should_still_add_the_retired_restriction_column() => _column.Name.ShouldEqual("SourceNamespaces");
    [Fact] void should_leave_the_column_nullable_because_nothing_reads_it_any_more() => _column.IsNullable.ShouldBeTrue();
    [Fact] void should_target_the_subscription_table() => _column.Table.ShouldEqual(WellKnownTableNames.EventStoreSubscriptions);
}
