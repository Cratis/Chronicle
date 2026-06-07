// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

#pragma warning disable CA2100 // Table names come from internal constants, not user input.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelMigrator.given;

/// <summary>
/// Establishes a real named shared-cache in-memory SQLite database together with a real
/// <see cref="TableMigrator{TContext}"/> and <see cref="ReadModelMigrator"/>.
/// A named shared-cache database is used instead of the default <c>:memory:</c> connection
/// so that the <see cref="TableMigrator{TContext}"/> can open and close its own transient
/// connections (as it does in production) while the master connection keeps the database alive.
/// </summary>
public class a_read_model_migrator : Specification
{
    string _connectionString;
    SqliteConnection _masterConnection;
    protected ReadModelMigrator _migrator;

    void Establish()
    {
        // Each Guid-named database is unique to this spec instance, so the TableMigrator's
        // static cache entries are naturally isolated between test runs.
        var dbName = $"rmm_{Guid.NewGuid():N}";
        _connectionString = $"DataSource={dbName};Mode=Memory;Cache=Shared";

        _masterConnection = new SqliteConnection(_connectionString);
        _masterConnection.Open();

        var tableMigrator = new TableMigrator<ReadModelDbContext>(
            Substitute.For<ILogger<TableMigrator<ReadModelDbContext>>>());

        _migrator = new ReadModelMigrator(
            tableMigrator,
            Substitute.For<ILogger<ReadModelMigrator>>());
    }

    void Destroy()
    {
        _masterConnection.Close();
        _masterConnection.Dispose();
    }

    /// <summary>
    /// Creates a <see cref="ReadModelDbContext"/> that targets the spec's named in-memory database.
    /// </summary>
    /// <param name="tableName">Name of the read model table.</param>
    /// <param name="columns">Column definitions derived from the read model schema.</param>
    /// <returns>A configured <see cref="ReadModelDbContext"/>.</returns>
    protected ReadModelDbContext CreateContext(string tableName, IReadOnlyList<ProjectedColumn> columns)
    {
        var options = new DbContextOptionsBuilder<ReadModelDbContext>()
            .UseSqlite(_connectionString)
            .AddConceptAsSupport()
            .Options;

        return new ReadModelDbContext(options, tableName, columns, _migrator);
    }

    /// <summary>
    /// Returns the column names that physically exist in the given table by querying
    /// SQLite's <c>PRAGMA table_info</c> via the master connection.
    /// Column name is at ordinal 1 (0 = cid, 1 = name, 2 = type, …).
    /// </summary>
    /// <param name="tableName">Name of the table to inspect.</param>
    /// <returns>A set of column names, compared case-insensitively.</returns>
    protected async Task<IReadOnlySet<string>> GetActualColumnNamesAsync(string tableName)
    {
        await using var command = _masterConnection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{tableName}\")";

        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(1));
        }

        return columns;
    }
}
