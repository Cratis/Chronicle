// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverAlertMigration;

public class when_upgrading_an_existing_schema : Specification
{
    IReadOnlyList<MigrationOperation> _operations;
    Dictionary<string, AddColumnOperation> _columns;

    void Because()
    {
        _operations = new Migrations.v19_15_0().UpOperations;
        _columns = _operations.OfType<AddColumnOperation>().ToDictionary(_ => _.Name);
    }

    [Fact] void should_only_add_columns() => _operations.All(_ => _ is AddColumnOperation).ShouldBeTrue();
    [Fact] void should_add_all_source_fields() => _columns.Keys.ShouldContainOnly("AlertLifecycleId", "AlertRevision", "AlertDisposition", "QuarantineEpisodeId");
    [Fact] void should_target_the_existing_observers_table() => _columns.Values.All(_ => _.Table == WellKnownTableNames.Observers).ShouldBeTrue();
    [Fact] void should_leave_legacy_lifecycles_uninitialized() => _columns["AlertLifecycleId"].DefaultValue.ShouldEqual(Guid.Empty);
    [Fact] void should_allow_legacy_quarantine_to_adopt_its_identity() => _columns["QuarantineEpisodeId"].IsNullable.ShouldBeTrue();
}
