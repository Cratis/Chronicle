// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.Schema;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner.when_preparing_cycle_receipts;

[Collection(SqlServerCollection.Name)]
public class and_receipts_are_prepared_on_sql_server(SqlServerFixture fixture) : for_Database.given.a_sql_server_database(fixture)
{
    const string Table = "chronicle_sink_cycle_receipts";
    const string Index = "IX_chronicle_sink_cycle_receipts_CreatedAt";
    SchemaReport _provisioned;
    SchemaReport _again;
    SchemaReport _missingTable;
    SchemaReport _missingIndex;
    SchemaReport _repaired;
    SchemaReport _unsafeDrift;
    Exception _unsafeError;
    long _rowsKept;
    long _indexes;
    long _driftedTables;

    async Task Because()
    {
        var provisioner = new SchemaProvisioner(Database, NullLogger<SchemaProvisioner>.Instance);
        var scope = new SchemaScope([Store], ["production"]);
        _provisioned = await provisioner.Migrate(scope);
        _again = await provisioner.Migrate(scope);
        var file = await ReceiptDatabase();

        await Execute(file, $"DROP TABLE {Table}");
        _missingTable = await provisioner.Verify(scope);
        await provisioner.Migrate(scope);

        await Execute(file, $"INSERT INTO {Table} ([Id], [TableName], [EventSequenceNumber]) VALUES ('00000000-0000-0000-0000-000000000001', 't', 1)");
        await Execute(file, $"DROP INDEX [{Index}] ON {Table}");
        _missingIndex = await provisioner.Verify(scope);
        await provisioner.Migrate(scope);
        _repaired = await provisioner.Verify(scope);
        _rowsKept = await Scalar(file, $"SELECT COUNT(*) FROM {Table}");
        _indexes = await Scalar(file, $"SELECT COUNT(*) FROM sys.indexes WHERE object_id = OBJECT_ID('{Table}') AND name = '{Index}'");

        await Execute(file, $"DROP TABLE {Table}");
        await Execute(file, $"CREATE TABLE {Table} ([Id] uniqueidentifier NOT NULL PRIMARY KEY, [TableName] nvarchar(450) NOT NULL, [EventSequenceNumber] bigint NOT NULL, [CreatedAt] bigint NOT NULL DEFAULT 0)");
        _unsafeError = await Catch.Exception(() => provisioner.Migrate(scope));
        _unsafeDrift = await provisioner.Verify(scope);
        _driftedTables = await Scalar(file, $"SELECT COUNT(*) FROM sys.default_constraints WHERE parent_object_id = OBJECT_ID('{Table}') AND definition IN ('((0))', '(0)')");
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
    [Fact] void should_leave_unsafe_drift_unchanged() => _driftedTables.ShouldEqual(1);

    async Task<string> ReceiptDatabase()
    {
        await using var cluster = await Database.Cluster();
        var names = await cluster.DbContext.DatabaseNames.Where(row => row.Kind != DatabaseKind.Cluster).Select(row => row.PhysicalName).ToArrayAsync();
        foreach (var name in names)
        {
            var connectionString = new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = name }.ConnectionString;
            if (await Scalar(connectionString, $"SELECT COUNT(*) FROM information_schema.tables WHERE table_name = '{Table}'") == 1)
            {
                return connectionString;
            }
        }

        throw new InvalidOperationException("No read-model database holds the receipt table.");
    }

    static async Task Execute(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
#pragma warning disable CA2100 // Specification-owned statements.
        await using var command = new SqlCommand(sql, connection);
#pragma warning restore CA2100
        await command.ExecuteNonQueryAsync();
    }

    static async Task<long> Scalar(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
#pragma warning disable CA2100 // Specification-owned statements.
        await using var command = new SqlCommand(sql, connection);
#pragma warning restore CA2100
        return Convert.ToInt64(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }
}
