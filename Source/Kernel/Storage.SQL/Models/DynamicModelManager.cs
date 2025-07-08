// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NJsonSchema;
using System.Text;

namespace Cratis.Chronicle.Storage.SQL.Models;

/// <summary>
/// Handles dynamic model creation and migration for projection entities.
/// </summary>
public class DynamicModelManager
{
    readonly DbContext _dbContext;
    readonly SqlStorageOptions _options;
    readonly Dictionary<string, Type> _entityTypes = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicModelManager"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="options">SQL storage options.</param>
    public DynamicModelManager(DbContext dbContext, SqlStorageOptions options)
    {
        _dbContext = dbContext;
        _options = options;
    }

    /// <summary>
    /// Ensure that the projection table exists for the given model.
    /// </summary>
    /// <param name="model">The projection model.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task EnsureProjectionTableExists(Model model)
    {
        var tableName = GetTableName(model.Name);
        
        if (await TableExists(tableName))
        {
            // Check if we need to update the schema
            await UpdateSchemaIfNeeded(model);
        }
        else
        {
            // Create new table
            await CreateTable(model);
        }
    }

    /// <summary>
    /// Generate the table name for a projection model.
    /// </summary>
    /// <param name="modelName">The model name.</param>
    /// <returns>The table name.</returns>
    public string GetTableName(ModelName modelName)
    {
        return $"projection_{modelName.Value.Replace('-', '_').Replace('.', '_')}";
    }

    async Task<bool> TableExists(string tableName)
    {
        var schemaName = _options.Schema;
        
        if (_options.ProviderType == SqlProviderType.SqlServer)
        {
            var sql = """
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @tableName
                """;
            
            var count = await _dbContext.Database.SqlQueryRaw<int>(sql, schemaName, tableName).FirstOrDefaultAsync();
            return count > 0;
        }
        else // PostgreSQL
        {
            var sql = """
                SELECT COUNT(*) 
                FROM information_schema.tables 
                WHERE table_schema = @schema AND table_name = @tableName
                """;
            
            var count = await _dbContext.Database.SqlQueryRaw<int>(sql, schemaName, tableName).FirstOrDefaultAsync();
            return count > 0;
        }
    }

    async Task CreateTable(Model model)
    {
        var generator = new SqlSchemaGenerator(_options.ProviderType, _options.Schema);
        var createSql = generator.GenerateCreateTableSql(model);
        
        await _dbContext.Database.ExecuteSqlRawAsync(createSql);
    }

    async Task UpdateSchemaIfNeeded(Model model)
    {
        // This is where we would implement schema migration logic
        // For now, we'll just ensure the table has the basic required columns
        
        var tableName = GetTableName(model.Name);
        var requiredColumns = new[]
        {
            "Id",
            "EventSequenceNumber", 
            "LastUpdated",
            "Data"
        };

        foreach (var column in requiredColumns)
        {
            if (!await ColumnExists(tableName, column))
            {
                await AddColumn(tableName, column);
            }
        }
    }

    async Task<bool> ColumnExists(string tableName, string columnName)
    {
        var schemaName = _options.Schema;
        
        if (_options.ProviderType == SqlProviderType.SqlServer)
        {
            var sql = """
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @tableName AND COLUMN_NAME = @columnName
                """;
            
            var count = await _dbContext.Database.SqlQueryRaw<int>(sql, schemaName, tableName, columnName).FirstOrDefaultAsync();
            return count > 0;
        }
        else // PostgreSQL
        {
            var sql = """
                SELECT COUNT(*) 
                FROM information_schema.columns 
                WHERE table_schema = @schema AND table_name = @tableName AND column_name = @columnName
                """;
            
            var count = await _dbContext.Database.SqlQueryRaw<int>(sql, schemaName, tableName, columnName).FirstOrDefaultAsync();
            return count > 0;
        }
    }

    async Task AddColumn(string tableName, string columnName)
    {
        var columnType = GetColumnType(columnName);
        var sql = $"ALTER TABLE {_options.Schema}.{tableName} ADD {columnName} {columnType}";
        
        await _dbContext.Database.ExecuteSqlRawAsync(sql);
    }

    string GetColumnType(string columnName)
    {
        return columnName switch
        {
            "Id" => _options.ProviderType == SqlProviderType.SqlServer ? "NVARCHAR(255)" : "VARCHAR(255)",
            "EventSequenceNumber" => "BIGINT",
            "LastUpdated" => _options.ProviderType == SqlProviderType.SqlServer ? "DATETIME2" : "TIMESTAMP",
            "Data" => _options.ProviderType == SqlProviderType.SqlServer ? "NVARCHAR(MAX)" : "TEXT",
            _ => _options.ProviderType == SqlProviderType.SqlServer ? "NVARCHAR(MAX)" : "TEXT"
        };
    }
}