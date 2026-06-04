// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Logging;

#pragma warning disable SA1204 // Static helpers placed after the public methods that drive them.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModelMigrator"/> that creates the read-model table
/// with the column shape derived from the read model's <see cref="Cratis.Chronicle.Schemas.JsonSchema"/>.
/// Each leaf property in the schema becomes a typed column; arrays and nested objects become a JSON
/// column (jsonb on PostgreSQL, nvarchar(max) on SQL Server, text on SQLite).
/// </summary>
/// <param name="tableMigrator">The <see cref="ITableMigrator{TContext}"/> for migrating tables.</param>
/// <param name="logger">The <see cref="ILogger{ReadModelMigrator}"/> for logging.</param>
public class ReadModelMigrator(
    ITableMigrator<ReadModelDbContext> tableMigrator,
    ILogger<ReadModelMigrator> logger) : IReadModelMigrator
{
    /// <inheritdoc/>
    public Task EnsureTableMigrated(string tableName, ReadModelDbContext context) =>
        tableMigrator.EnsureTableMigrated(tableName, context, CreateTable);

    /// <inheritdoc/>
    public void ClearMigrationCache(string connectionStringPrefix) =>
        tableMigrator.ClearMigrationCacheForConnectionString(connectionStringPrefix);

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
                Name = $"PK_{tableName}",
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
                nameof(Decimal) => "DECIMAL",
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
                nameof(Decimal) => "DECIMAL(18,2)",
                _ => "NVARCHAR(MAX)"
            },
            _ => unwrapped.Name switch
            {
                nameof(Single) or nameof(Double) or nameof(Decimal) => "REAL",
                _ => "INTEGER"
            }
        };
    }
}
