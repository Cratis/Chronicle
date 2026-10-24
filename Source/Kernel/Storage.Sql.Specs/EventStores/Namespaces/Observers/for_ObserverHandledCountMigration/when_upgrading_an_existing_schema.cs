// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverHandledCountMigration;

public class when_upgrading_an_existing_schema : Specification
{
    IReadOnlyList<MigrationOperation> _operations;
    Dictionary<string, AddColumnOperation> _columns;

    void Because()
    {
        _operations = new Migrations.v19_110_0 { ActiveProvider = "Microsoft.EntityFrameworkCore.Sqlite" }.UpOperations;
        _columns = _operations.OfType<AddColumnOperation>().ToDictionary(_ => _.Name);
    }

    [Fact] void should_only_add_columns() => _operations.All(_ => _ is AddColumnOperation).ShouldBeTrue();
    [Fact] void should_add_both_handled_count_columns() => _columns.Keys.ShouldContainOnly("HandledEventCount", "HandledEventCountPerEventType");
    [Fact] void should_target_the_existing_observers_table() => _columns.Values.All(_ => _.Table == WellKnownTableNames.Observers).ShouldBeTrue();
    [Fact] void should_require_the_handled_event_count() => _columns["HandledEventCount"].IsNullable.ShouldBeFalse();
    [Fact] void should_default_the_handled_event_count_to_zero() => _columns["HandledEventCount"].DefaultValue.ShouldEqual(0UL);
    [Fact] void should_allow_legacy_counts_per_event_type_to_be_absent() => _columns["HandledEventCountPerEventType"].IsNullable.ShouldBeTrue();
}
