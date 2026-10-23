// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.Schema;

/// <summary>
/// Checks migration history without invoking an EF migrator or taking a schema lock.
/// </summary>
/// <param name="options">The context options.</param>
/// <param name="assembly">The known migrations.</param>
/// <param name="history">The migration history.</param>
/// <param name="creator">The database existence probe.</param>
/// <param name="connection">The database connection.</param>
/// <param name="currentContext">The context being verified.</param>
internal sealed class SchemaVerification(
    IDbContextOptions options,
    IMigrationsAssembly assembly,
    IHistoryRepository history,
    IDatabaseCreator creator,
    IRelationalConnection connection,
    ICurrentDbContext currentContext)
{
    /// <summary>
    /// Gets whether migration must be replaced by verification.
    /// </summary>
    internal bool Required => options.FindExtension<SchemaModeOptionsExtension>()?.Mode == SchemaMode.Verify;

    /// <summary>
    /// Verifies that every known migration has been applied.
    /// </summary>
    /// <exception cref="SchemaNotCurrent">The database is missing or has pending migrations.</exception>
    internal void Verify()
    {
        if (!((IRelationalDatabaseCreator)creator).Exists())
        {
            throw new SchemaNotCurrent(connection.DbConnection.Database, currentContext.Context.GetType().Name, ["database missing"]);
        }

        Check(history.GetAppliedMigrations());
    }

    /// <summary>
    /// Verifies that every known migration has been applied asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The verification task.</returns>
    /// <exception cref="SchemaNotCurrent">The database is missing or has pending migrations.</exception>
    internal async Task VerifyAsync(CancellationToken cancellationToken)
    {
        if (!await ((IRelationalDatabaseCreator)creator).ExistsAsync(cancellationToken))
        {
            throw new SchemaNotCurrent(connection.DbConnection.Database, currentContext.Context.GetType().Name, ["database missing"]);
        }

        Check(await history.GetAppliedMigrationsAsync(cancellationToken));
    }

    void Check(IReadOnlyList<HistoryRow> applied)
    {
        var pending = assembly.Migrations.Keys.Except(applied.Select(row => row.MigrationId), StringComparer.Ordinal).ToArray();
        if (pending.Length != 0)
        {
            throw new SchemaNotCurrent(connection.DbConnection.Database, currentContext.Context.GetType().Name, pending);
        }
    }
}
