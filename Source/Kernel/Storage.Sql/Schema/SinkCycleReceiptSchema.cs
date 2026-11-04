// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sql.Schema;

/// <summary>
/// The definition, read-only validation and explicit offline provisioning of the sink cycle receipt table.
/// </summary>
/// <remarks>
/// In <see cref="SchemaMode.Verify"/> the runtime only validates through catalog reads and never issues DDL.
/// Creating the table, or adding its missing expiry index, is reserved for explicit offline provisioning.
/// </remarks>
internal static partial class SinkCycleReceiptSchema
{
    const string Table = WellKnownTableNames.SinkCycleReceipts;
    static readonly string[] _columns = ["Id", "TableName", "EventSequenceNumber", "CreatedAt"];

    /// <summary>
    /// Gets the name of the expiry index.
    /// </summary>
    internal static string IndexName => $"IX_{Table}_CreatedAt";

    /// <summary>
    /// Gets whether the actual context is in <see cref="SchemaMode.Verify"/>. An absent policy is Migrate.
    /// </summary>
    /// <param name="context">The context whose own options decide.</param>
    /// <returns>True when only read-only checks are permitted.</returns>
    internal static bool IsVerify(DbContext context) =>
        context.GetService<IDbContextOptions>().FindExtension<SchemaModeOptionsExtension>()?.Mode == SchemaMode.Verify;

    /// <summary>
    /// Builds the unchanged definition of the receipt table and its expiry index.
    /// </summary>
    /// <param name="providerName">The EF provider name.</param>
    /// <param name="tableName">The table name.</param>
    /// <param name="databaseType">The database type.</param>
    /// <returns>The migration builder holding the create operations.</returns>
    internal static MigrationBuilder CreateOperations(string? providerName, string tableName, DatabaseType databaseType)
    {
        var migrationBuilder = new MigrationBuilder(providerName);
        var nowSql = SinkCycleReceipt.NowSql(databaseType);
        migrationBuilder.CreateTable(
            name: tableName,
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                TableName = table.StringColumn(migrationBuilder, nullable: false),
                EventSequenceNumber = table.Column<long>(nullable: false),
                CreatedAt = table.Column<long>(nullable: false, defaultValueSql: nowSql)
            },
            constraints: table => table.PrimaryKey($"PK_{tableName}", receipt => receipt.Id));
        migrationBuilder.CreateIndex($"IX_{tableName}_CreatedAt", tableName, "CreatedAt");
        return migrationBuilder;
    }

    /// <summary>
    /// Validates the receipt table with read-only catalog reads.
    /// </summary>
    /// <param name="context">The context to read through, using its connection and current transaction.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="SchemaNotCurrent">The table is missing, drifted or could not be inspected.</exception>
    internal static async Task Validate(DbContext context)
    {
        var inspection = await Inspect(context);
        if (!inspection.IsCurrent)
        {
            throw new SchemaNotCurrent(context.Database.GetDbConnection().Database, Table, inspection.All);
        }
    }

    /// <summary>
    /// Provisions the receipt table, or only its missing expiry index, inside the schema lock. Unsafe drift is never changed.
    /// </summary>
    /// <param name="context">The offline schema context.</param>
    /// <param name="serviceProvider">The services holding the table migrator.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="SchemaNotCurrent">The existing table has drift that is unsafe to repair.</exception>
    internal static Task Provision(DbContext context, IServiceProvider serviceProvider) =>
        DatabaseSchemaLock.Run(context, async () =>
        {
            var inspection = await Inspect(context);
            if (inspection.Unsafe.Count > 0)
            {
                throw new SchemaNotCurrent(context.Database.GetDbConnection().Database, Table, inspection.Unsafe);
            }

            if (inspection.IsCurrent)
            {
                return;
            }

            var databaseType = context.Database.GetDatabaseType();
            MigrationBuilder operations;
            if (inspection.TableMissing)
            {
                operations = CreateOperations(context.Database.ProviderName, Table, databaseType);
            }
            else
            {
                operations = new MigrationBuilder(context.Database.ProviderName);
                operations.CreateIndex(IndexName, Table, "CreatedAt");
            }

            await serviceProvider.GetRequiredService<ITableMigrator<DbContext>>().ExecuteMigrationOperations(context, operations);
        });
}
