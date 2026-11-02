// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Cycle = Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction.given;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_validating_cycle_receipts.given;

/// <summary>
/// A sink whose read model is provisioned under Migrate and whose cycles can then run under Verify, chosen by
/// <see cref="_verify"/> when each cycle opens its own context, as the runtime does.
/// </summary>
/// <typeparam name="THarness">The database the sink writes to.</typeparam>
public abstract class a_receipt_cycle<THarness> : Cycle.a_cycle_with_failure_injection<THarness>
    where THarness : ISqlSinkHarness
{
    static readonly string[] _catalogMarkers = ["pragma_table_xinfo", "pg_attribute", "sys.columns"];
    protected bool _verify;

    protected IEnumerable<string> CatalogReads => _capture.Commands.Where(command => _catalogMarkers.Any(marker => command.Contains(marker, StringComparison.OrdinalIgnoreCase)));

    protected IEnumerable<string> ReceiptDdl => ReceiptCommands.Where(command => new[] { "CREATE TABLE", "CREATE INDEX", "ALTER TABLE", "DROP TABLE", "DROP INDEX" }.Any(verb => command.Contains(verb, StringComparison.OrdinalIgnoreCase)));

    protected void Configure(DbContextOptionsBuilder<ReadModelDbContext> builder)
    {
        Cycle.retrying_on_timeout.Configure(builder);
        if (_verify)
        {
            ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(new SchemaModeOptionsExtension(SchemaMode.Verify));
        }
    }

    /// <summary>
    /// Creates the read model row the cycles join to, with a single write that never needs a receipt.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected Task Seed() => Apply("target", 1, Set(("joinId", "J")));

    protected Task MultiWrite(ISink sink, string id, ulong sequenceNumber) =>
        ApplyOn(sink, id, sequenceNumber, KeyedFromJoin("joinId", "J", ("x", 7)), Set(("group", "direct")));

    /// <summary>
    /// Runs a multi-write cycle under Migrate, which provisions the receipt table.
    /// </summary>
    /// <param name="sink">The sink.</param>
    /// <param name="id">The key.</param>
    /// <param name="sequenceNumber">The sequence number.</param>
    /// <returns>Awaitable task.</returns>
    protected async Task Provision(ISink sink, string id, ulong sequenceNumber)
    {
        _verify = false;
        await MultiWrite(sink, id, sequenceNumber);
    }

    protected async Task<Exception?> Attempt(ISink sink, string id, ulong sequenceNumber)
    {
        try
        {
            await MultiWrite(sink, id, sequenceNumber);
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    protected async Task<bool> ReceiptTableExists()
    {
        var sql = _harness.ConnectionString.GetDatabaseType() switch
        {
            DatabaseType.PostgreSql => $"SELECT CASE WHEN to_regclass('\"{ReceiptTable}\"') IS NULL THEN 0 ELSE 1 END AS \"Value\"",
            DatabaseType.SqlServer => $"SELECT CASE WHEN OBJECT_ID(N'{ReceiptTable}') IS NULL THEN 0 ELSE 1 END AS [Value]",
            _ => $"SELECT COUNT(*) AS \"Value\" FROM sqlite_master WHERE type = 'table' AND name = '{ReceiptTable}'"
        };
        return await Scalar(sql) == 1;
    }

    protected Task DropReceiptTable() => Execute($"DROP TABLE {Quote(ReceiptTable)}");
}
