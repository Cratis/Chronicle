// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Logging;
using Npgsql;

#pragma warning disable SA1204 // Static helpers placed after the public methods that drive them.
#pragma warning disable CA2100 // Table and column names are internal constants, not user input.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModelMigrator"/> that creates the read-model table
/// with the column shape derived from the read model's <see cref="Schemas.JsonSchema"/>.
/// Each leaf property in the schema becomes a typed column; arrays and nested objects become a JSON
/// column (jsonb on PostgreSQL, nvarchar(max) on SQL Server, text on SQLite).
/// </summary>
/// <param name="tableMigrator">The <see cref="ITableMigrator{TContext}"/> for migrating tables.</param>
/// <param name="logger">The <see cref="ILogger{ReadModelMigrator}"/> for logging.</param>
public class ReadModelMigrator(
    ITableMigrator<ReadModelDbContext> tableMigrator,
    ILogger<ReadModelMigrator> logger) : IReadModelMigrator
{
    static readonly ConcurrentDictionary<string, bool> _columnMigrations = new();

    /// <inheritdoc/>
    public async Task EnsureTableMigrated(string tableName, ReadModelDbContext context)
    {
        await tableMigrator.EnsureTableMigrated(tableName, context, CreateTable);
        await EnsureMissingColumnsAdded(tableName, context);
    }

    /// <inheritdoc/>
    public async Task EnsureReplayPromotions(ReadModelDbContext context)
    {
        try
        {
            await tableMigrator.EnsureTableMigrated(WellKnownTableNames.ReplayPromotions, context, CreateReplayPromotions);
        }
        catch (Exception exception) when (IsDuplicateTable(exception))
        {
            // Another silo may have created the shared marker table. Recheck through the migrator;
            // do not cache success merely because a CREATE failed.
            await tableMigrator.EnsureTableMigrated(WellKnownTableNames.ReplayPromotions, context, CreateReplayPromotions);
        }
    }

    /// <inheritdoc/>
    public void ClearMigrationCache(string connectionStringPrefix)
    {
        tableMigrator.ClearMigrationCacheForConnectionString(connectionStringPrefix);
        foreach (var key in _columnMigrations.Keys.Where(k => k.StartsWith(connectionStringPrefix, StringComparison.OrdinalIgnoreCase)))
        {
            _columnMigrations.TryRemove(key, out _);
        }
    }

    async Task CreateReplayPromotions(ReadModelDbContext context, string tableName)
    {
        var migrationBuilder = new MigrationBuilder(context.Database.ProviderName);
        migrationBuilder.CreateTable(
            name: tableName,
            columns: table => new
            {
                Id = table.StringColumn(migrationBuilder, maxLength: 64, nullable: false),
                ContainerName = table.StringColumn(migrationBuilder, nullable: false),
                RevertContainerName = table.StringColumn(migrationBuilder, nullable: false),
                BackupTableName = table.StringColumn(migrationBuilder, nullable: true)
            },
            constraints: table => table.PrimaryKey($"PK_{tableName}", promotion => promotion.Id));
        await tableMigrator.ExecuteMigrationOperations(context, migrationBuilder);
    }

    async Task EnsureMissingColumnsAdded(string tableName, ReadModelDbContext context)
    {
        // Build a cache key that covers this connection string, table, and exact column set.
        // When the schema changes the key changes and we re-check the database.
        var connectionString = context.Database.GetConnectionString() ?? string.Empty;
        var columnsKey = string.Join('|', context.Columns.Select(c => c.Name));
        var cacheKey = $"{connectionString}:{tableName}:{columnsKey}";

        if (_columnMigrations.ContainsKey(cacheKey))
        {
            return;
        }

        var existingColumns = await GetExistingColumnNames(context, tableName);
        var missingColumns = context.Columns
            .Where(col => !existingColumns.Contains(col.Name))
            .ToList();

        if (missingColumns.Count > 0)
        {
            logger.AddingMissingColumns(missingColumns.Count, tableName, string.Join(',', missingColumns.Select(c => c.Name)));

            foreach (var column in missingColumns)
            {
                // Each addition has its own transaction so a duplicate cannot roll back or skip other columns.
                var migrationBuilder = new MigrationBuilder(context.Database.ProviderName);
                var op = BuildAddColumnOperation(column, migrationBuilder.GetDatabaseType(), tableName);
                if (column.Name != WellKnownProperties.ReadModelInstanceInitialized)
                {
                    op.IsNullable = true;
                }
                migrationBuilder.Operations.Add(op);

                try
                {
                    await tableMigrator.ExecuteMigrationOperations(context, migrationBuilder);
                }
                catch (Exception exception) when (IsDuplicateColumn(exception))
                {
                    // Another kernel added the column after our schema snapshot. Adopt only that column.
                    if (!await tableMigrator.ColumnExists(context, tableName, column.Name))
                    {
                        throw;
                    }
                }
            }
        }

        _columnMigrations.TryAdd(cacheKey, true);
    }

    static bool IsDuplicateTable(Exception exception) => exception switch
    {
        PostgresException postgres => postgres.SqlState == PostgresErrorCodes.DuplicateTable || postgres.SqlState == PostgresErrorCodes.UniqueViolation,
        SqlException sql => sql.Number == 2714,
        SqliteException sqlite => sqlite.SqliteErrorCode == 1 && sqlite.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase),
        _ => false
    };

    static bool IsDuplicateColumn(Exception exception) => exception switch
    {
        PostgresException postgres => postgres.SqlState == PostgresErrorCodes.DuplicateColumn,
        SqlException sql => sql.Number == 2705,
        SqliteException sqlite => sqlite.SqliteErrorCode == 1 && sqlite.Message.Contains("duplicate column name:", StringComparison.OrdinalIgnoreCase),
        _ => false
    };

    static async Task<HashSet<string>> GetExistingColumnNames(ReadModelDbContext context, string tableName)
    {
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        try
        {
            await using var command = connection.CreateCommand();
            int nameOrdinal;

            if (context.Database.IsNpgsql())
            {
                command.CommandText = $"SELECT column_name FROM information_schema.columns WHERE table_schema = 'public' AND table_name = '{tableName}'";
                nameOrdinal = 0;
            }
            else if (context.Database.IsSqlServer())
            {
                command.CommandText = $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}'";
                nameOrdinal = 0;
            }
            else
            {
                // SQLite PRAGMA table_info returns: cid, name, type, notnull, dflt_value, pk
                command.CommandText = $"PRAGMA table_info(\"{tableName}\")";
                nameOrdinal = 1;
            }

            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                columns.Add(reader.GetString(nameOrdinal));
            }

            return columns;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    async Task CreateTable(ReadModelDbContext context, string tableName)
    {
        logger.CreatingReadModelTable(tableName);

        var migrationBuilder = new MigrationBuilder(context.Database.ProviderName);
        var databaseType = migrationBuilder.GetDatabaseType();

        var createTable = new CreateTableOperation
        {
            Name = tableName
        };

        ProjectedColumn? key = null;
        foreach (var column in context.Columns)
        {
            createTable.Columns.Add(BuildAddColumnOperation(column, databaseType, tableName));
            if (column.IsKey)
            {
                key = column;
            }
        }

        if (key is not null)
        {
            createTable.PrimaryKey = new AddPrimaryKeyOperation
            {
                Name = PrimaryKeyNames.For(databaseType, tableName),
                Table = tableName,
                Columns = [key.Name]
            };
        }

        migrationBuilder.Operations.Add(createTable);

        await tableMigrator.ExecuteMigrationOperations(context, migrationBuilder);
    }

    static AddColumnOperation BuildAddColumnOperation(ProjectedColumn column, DatabaseType databaseType, string tableName)
    {
        var op = new AddColumnOperation
        {
            Name = column.Name,
            Table = tableName,
            ClrType = MapClrType(column),
            ColumnType = GetColumnType(column, databaseType),
            IsNullable = column.IsNullable && !column.IsKey
        };

        if (column.Name == WellKnownProperties.ReadModelInstanceInitialized)
        {
            // Older SQL rows have no evidence of being placeholders. Treating them as uninitialized
            // would replace legitimately null properties with initial values on their next root event.
            op.DefaultValue = true;
        }

        if (column.IsJson)
        {
            op.AddAnnotation(JsonColumnMigrationExtensions.CratisColumnTypeAnnotation, JsonColumnMigrationExtensions.JsonColumnType);
        }

        return op;
    }

    static Type MapClrType(ProjectedColumn column)
    {
        if (column.IsJson)
        {
            return typeof(string);
        }

        // SQLite has no native Guid type; the EF model uses a string-converted Guid for SQLite (see
        // ReadModelDbContext) but the migration column type itself is still chosen from the
        // ColumnTypeMappings table — we keep ClrType=Guid here so the EF model and the migration agree.
        return column.ClrType;
    }

    static string? GetColumnType(ProjectedColumn column, DatabaseType databaseType)
    {
        if (column.IsJson)
        {
            return databaseType switch
            {
                DatabaseType.PostgreSql => "jsonb",
                DatabaseType.SqlServer => "nvarchar(max)",
                _ => "TEXT"
            };
        }

        var clr = column.ClrType;
        if (clr == typeof(string))
        {
            return databaseType switch
            {
                DatabaseType.PostgreSql => column.IsKey ? "VARCHAR(200)" : "TEXT",
                DatabaseType.SqlServer => column.IsKey ? "NVARCHAR(200)" : "NVARCHAR(MAX)",
                _ => "TEXT"
            };
        }

        if (clr == typeof(Guid) || clr == typeof(Guid?))
        {
            return databaseType switch
            {
                DatabaseType.PostgreSql => "UUID",
                DatabaseType.SqlServer => "UNIQUEIDENTIFIER",
                _ => "TEXT"
            };
        }

        if (clr == typeof(bool) || clr == typeof(bool?))
        {
            return databaseType switch
            {
                DatabaseType.PostgreSql => "BOOLEAN",
                DatabaseType.SqlServer => "BIT",
                _ => "INTEGER"
            };
        }

        if (clr == typeof(DateTimeOffset) || clr == typeof(DateTimeOffset?))
        {
            return databaseType switch
            {
                DatabaseType.PostgreSql => "TIMESTAMPTZ",
                DatabaseType.SqlServer => "DATETIMEOFFSET",
                _ => "TEXT"
            };
        }

        if (clr == typeof(DateTime) || clr == typeof(DateTime?))
        {
            return databaseType switch
            {
                DatabaseType.PostgreSql => "TIMESTAMP",
                DatabaseType.SqlServer => "DATETIME2",
                _ => "TEXT"
            };
        }

        if (clr == typeof(DateOnly) || clr == typeof(DateOnly?))
        {
            return databaseType switch
            {
                DatabaseType.PostgreSql => "DATE",
                DatabaseType.SqlServer => "DATE",
                _ => "TEXT"
            };
        }

        if (clr == typeof(TimeOnly) || clr == typeof(TimeOnly?))
        {
            return databaseType switch
            {
                DatabaseType.PostgreSql => "TIME",
                DatabaseType.SqlServer => "TIME",
                _ => "TEXT"
            };
        }

        // Numeric types — match the same mapping Arc's NumberColumn helper produces.
        var unwrapped = Nullable.GetUnderlyingType(clr) ?? clr;
        return databaseType switch
        {
            DatabaseType.PostgreSql => unwrapped.Name switch
            {
                nameof(Byte) or nameof(SByte) or nameof(Int16) => "SMALLINT",
                nameof(UInt16) or nameof(Int32) => "INTEGER",
                nameof(UInt32) or nameof(Int64) => "BIGINT",
                nameof(UInt64) => "NUMERIC(20,0)",
                nameof(Single) => "REAL",
                nameof(Double) => "DOUBLE PRECISION",
                nameof(Decimal) => "NUMERIC",
                _ => "TEXT"
            },
            DatabaseType.SqlServer => unwrapped.Name switch
            {
                nameof(Byte) => "TINYINT",
                nameof(SByte) or nameof(Int16) => "SMALLINT",
                nameof(UInt16) or nameof(Int32) => "INT",
                nameof(UInt32) or nameof(Int64) => "BIGINT",
                nameof(UInt64) => "DECIMAL(20,0)",
                nameof(Single) => "REAL",
                nameof(Double) => "FLOAT",
                nameof(Decimal) => "DECIMAL(38,18)",
                _ => "NVARCHAR(MAX)"
            },
            _ => unwrapped.Name switch
            {
                nameof(Single) or nameof(Double) => "REAL",
                nameof(Decimal) => "TEXT",
                _ => "INTEGER"
            }
        };
    }
}
