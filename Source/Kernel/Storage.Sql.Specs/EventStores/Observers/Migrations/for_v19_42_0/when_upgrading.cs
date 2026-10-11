// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Observers.Migrations.for_v19_42_0;

public class when_upgrading : Specification
{
    AddColumnOperation _column;
    void Because() => _column = new v19_42_0().UpOperations.OfType<AddColumnOperation>().Single();
    [Fact] void should_sort_after_observer_definition_creation() => string.CompareOrdinal(typeof(v19_42_0).GetCustomAttributes(false).OfType<MigrationAttribute>().Single().Id, typeof(v15_0_0).GetCustomAttributes(false).OfType<MigrationAttribute>().Single().Id).ShouldBeGreaterThan(0);
    [Fact] void should_add_the_delivery_policy_column() => _column.Name.ShouldEqual(nameof(ObserverDefinition.GenerationDelivery));
    [Fact] void should_keep_existing_observers_in_compatibility_mode() => _column.DefaultValue.ShouldEqual(0);
    [Fact] void should_target_the_observer_definition_table() => _column.Table.ShouldEqual(WellKnownTableNames.ObserverDefinitions);
}
