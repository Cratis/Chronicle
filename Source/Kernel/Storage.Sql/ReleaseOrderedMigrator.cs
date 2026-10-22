// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
#pragma warning disable EF1001 // The relational migrator is the only place EF Core decides the order migrations run in; specs pin the override.
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Represents the relational <see cref="IMigrator"/> that applies migrations in the order of the releases they were written for.
/// </summary>
/// <remarks>
/// Used for SQLite and SQL Server; see <see cref="ReleaseOrderedMigrations"/>.
/// </remarks>
/// <param name="migrationsAssembly">The <see cref="IMigrationsAssembly"/>.</param>
/// <param name="historyRepository">The <see cref="IHistoryRepository"/>.</param>
/// <param name="databaseCreator">The <see cref="IDatabaseCreator"/>.</param>
/// <param name="migrationsSqlGenerator">The <see cref="IMigrationsSqlGenerator"/>.</param>
/// <param name="rawSqlCommandBuilder">The <see cref="IRawSqlCommandBuilder"/>.</param>
/// <param name="migrationCommandExecutor">The <see cref="IMigrationCommandExecutor"/>.</param>
/// <param name="connection">The <see cref="IRelationalConnection"/>.</param>
/// <param name="sqlGenerationHelper">The <see cref="ISqlGenerationHelper"/>.</param>
/// <param name="currentContext">The <see cref="ICurrentDbContext"/>.</param>
/// <param name="modelRuntimeInitializer">The <see cref="IModelRuntimeInitializer"/>.</param>
/// <param name="logger">The migrations <see cref="IDiagnosticsLogger{T}"/>.</param>
/// <param name="commandLogger">The <see cref="IRelationalCommandDiagnosticsLogger"/>.</param>
/// <param name="databaseProvider">The <see cref="IDatabaseProvider"/>.</param>
/// <param name="migrationsModelDiffer">The <see cref="IMigrationsModelDiffer"/>.</param>
/// <param name="designTimeModel">The <see cref="IDesignTimeModel"/>.</param>
/// <param name="contextOptions">The <see cref="IDbContextOptions"/>.</param>
/// <param name="executionStrategy">The <see cref="IExecutionStrategy"/>.</param>
public class ReleaseOrderedMigrator(
    IMigrationsAssembly migrationsAssembly,
    IHistoryRepository historyRepository,
    IDatabaseCreator databaseCreator,
    IMigrationsSqlGenerator migrationsSqlGenerator,
    IRawSqlCommandBuilder rawSqlCommandBuilder,
    IMigrationCommandExecutor migrationCommandExecutor,
    IRelationalConnection connection,
    ISqlGenerationHelper sqlGenerationHelper,
    ICurrentDbContext currentContext,
    IModelRuntimeInitializer modelRuntimeInitializer,
    IDiagnosticsLogger<DbLoggerCategory.Migrations> logger,
    IRelationalCommandDiagnosticsLogger commandLogger,
    IDatabaseProvider databaseProvider,
    IMigrationsModelDiffer migrationsModelDiffer,
    IDesignTimeModel designTimeModel,
    IDbContextOptions contextOptions,
    IExecutionStrategy executionStrategy)
    : Migrator(
        migrationsAssembly,
        historyRepository,
        databaseCreator,
        migrationsSqlGenerator,
        rawSqlCommandBuilder,
        migrationCommandExecutor,
        connection,
        sqlGenerationHelper,
        currentContext,
        modelRuntimeInitializer,
        logger,
        commandLogger,
        databaseProvider,
        migrationsModelDiffer,
        designTimeModel,
        contextOptions,
        executionStrategy)
{
    /// <inheritdoc/>
    protected override void PopulateMigrations(IEnumerable<string> appliedMigrationEntries, string? targetMigration, out MigratorData parameters)
    {
        base.PopulateMigrations(appliedMigrationEntries, targetMigration, out var decided);
        parameters = ReleaseOrderedMigrations.InReleaseOrder(decided);
    }
}
