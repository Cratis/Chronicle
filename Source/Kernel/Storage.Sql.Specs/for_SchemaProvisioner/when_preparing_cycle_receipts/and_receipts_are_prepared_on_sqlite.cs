// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Schema;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner.when_preparing_cycle_receipts;

public class and_receipts_are_prepared_on_sqlite : given.receipt_provisioning
{
    const string Index = "IX_chronicle_sink_cycle_receipts_CreatedAt";
    SchemaReport _provisioned;
    SchemaReport _again;
    SchemaReport _missingTable;
    SchemaReport _missingIndex;
    SchemaReport _repaired;
    SchemaReport _unsafeDrift;
    Exception _unsafeError;
    Exception _uniqueError;
    SchemaReport _uniqueDrift;
    long _rowsKept;
    long _indexes;
    long _driftedTables;

    async Task Because()
    {
        _provisioned = await _provisioner.Migrate(Scope);
        _again = await _provisioner.Migrate(Scope);
        var file = ReadModelDatabase;

        Execute(file, $"DROP TABLE {Table}");
        _missingTable = await _provisioner.Verify(Scope);
        await _provisioner.Migrate(Scope);

        Execute(file, $"INSERT INTO {Table} (Id, TableName, EventSequenceNumber) VALUES ('00000000-0000-0000-0000-000000000001', 't', 1)");
        Execute(file, $"DROP INDEX {Index}");
        _missingIndex = await _provisioner.Verify(Scope);
        await _provisioner.Migrate(Scope);
        _repaired = await _provisioner.Verify(Scope);
        _rowsKept = Scalar(file, $"SELECT COUNT(*) FROM {Table}");
        _indexes = Scalar(file, $"SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = '{Index}'");

        Execute(file, $"DROP TABLE {Table}");
        Execute(file, $"CREATE TABLE {Table} (Id TEXT NOT NULL PRIMARY KEY, TableName TEXT NOT NULL, EventSequenceNumber INTEGER NOT NULL, CreatedAt INTEGER NOT NULL DEFAULT 0)");
        _unsafeError = await Catch.Exception(() => _provisioner.Migrate(Scope));
        _unsafeDrift = await _provisioner.Verify(Scope);
        _driftedTables = Scalar(file, $"SELECT COUNT(*) FROM pragma_table_xinfo('{Table}') WHERE name = 'CreatedAt' AND dflt_value = '0'");
        Execute(file, $"DROP TABLE {Table}");
        await _provisioner.Migrate(Scope);
        Execute(file, $"CREATE UNIQUE INDEX ux_receipt_extra ON {Table} (EventSequenceNumber)");
        _uniqueError = await Catch.Exception(() => _provisioner.Migrate(Scope));
        _uniqueDrift = await _provisioner.Verify(Scope);
    }

    [Fact] void should_provision_the_receipts() => _provisioned.IsCurrent.ShouldBeTrue();
    [Fact] void should_be_idempotent() => _again.IsCurrent.ShouldBeTrue();
    [Fact] void should_report_a_missing_table() => _missingTable.IsCurrent.ShouldBeFalse();
    [Fact] void should_report_a_missing_index() => _missingIndex.IsCurrent.ShouldBeFalse();
    [Fact] void should_repair_the_index_offline() => _repaired.IsCurrent.ShouldBeTrue();
    [Fact] void should_keep_the_receipt_rows_when_repairing() => _rowsKept.ShouldEqual(1);
    [Fact] void should_have_one_expiry_index() => _indexes.ShouldEqual(1);
    [Fact] void should_refuse_to_repair_unsafe_drift() => _unsafeError.ShouldBeOfExactType<SchemaNotCurrent>();
    [Fact] void should_report_unsafe_drift() => _unsafeDrift.IsCurrent.ShouldBeFalse();
    [Fact] void should_refuse_to_provision_beside_a_unique_index() => _uniqueError.ShouldBeOfExactType<SchemaNotCurrent>();
    [Fact] void should_report_a_unique_extra_index_as_drift() => _uniqueDrift.IsCurrent.ShouldBeFalse();
    [Fact] void should_leave_unsafe_drift_unchanged() => _driftedTables.ShouldEqual(1);
}
