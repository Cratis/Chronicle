// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction.given;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_upgrading_an_existing_database.given;

/// <summary>
/// A database written by single-write cycles only, as the code before receipts existed would leave it, which is then opened by a
/// build that runs multi-write cycles. The stages can be run separately, by different builds, against one database file with
/// CHRONICLE_SINK_PROOF_STAGE set to seed or upgrade. Without it both stages run in one temporary database.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class an_existing_database<THarness> : a_cycle_with_failure_injection<THarness>
    where THarness : ISqlSinkHarness
{
    /// <summary>
    /// Gets the stage being run, which is seed, upgrade or both.
    /// </summary>
    protected static string Stage => Environment.GetEnvironmentVariable("CHRONICLE_SINK_PROOF_STAGE") ?? "both";

    /// <summary>
    /// Counts the receipt tables.
    /// </summary>
    /// <returns>One when the table exists, otherwise zero.</returns>
    protected Task<long> ReceiptTables() => Scalar(IsSqlite
        ? $"SELECT CAST(COUNT(*) AS bigint) AS \"Value\" FROM sqlite_master WHERE name = '{ReceiptTable}'"
        : $"SELECT CAST(COUNT(*) AS bigint) AS \"Value\" FROM information_schema.tables WHERE table_name = '{ReceiptTable}'");

    /// <summary>
    /// Counts the stored rows.
    /// </summary>
    /// <returns>The number of rows.</returns>
    protected Task<long> Rows() => Scalar($"SELECT CAST(COUNT(*) AS bigint) AS \"Value\" FROM {Quote(TableName)}");
}
