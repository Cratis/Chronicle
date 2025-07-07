// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Models;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Json;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.SQL.Sinks;

/// <summary>
/// Represents an implementation of <see cref="ISink"/> for working with projections in SQL databases.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Sink"/> class.
/// </remarks>
/// <param name="model">The <see cref="Model"/> the sink is for.</param>
/// <param name="dbContext">The <see cref="DbContext"/> for database operations.</param>
/// <param name="changesetConverter">The <see cref="IChangesetConverter"/> for converting changesets.</param>
/// <param name="expandoObjectConverter">The <see cref="IExpandoObjectConverter"/> for converting between objects.</param>
public class Sink(
    Model model,
    DbContext dbContext,
    IChangesetConverter changesetConverter,
    IExpandoObjectConverter expandoObjectConverter) : ISink
{
    /// <inheritdoc/>
    public SinkTypeName Name => "SQL";

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.SQL;

    /// <inheritdoc/>
    public async Task<ExpandoObject?> FindOrDefault(Key key)
    {
        var tableName = GetTableName();
        var keyColumnName = GetKeyColumnName();
        
        var sql = $"SELECT * FROM {tableName} WHERE {keyColumnName} = @key";
        
        // This is a simplified implementation - in practice we'd need more sophisticated
        // dynamic query building based on the model schema
        var connection = dbContext.Database.GetDbConnection();
        await dbContext.Database.OpenConnectionAsync();
        
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@key";
        parameter.Value = key.Value;
        command.Parameters.Add(parameter);
        
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var result = new ExpandoObject();
            var dictionary = (IDictionary<string, object?>)result;
            
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                dictionary[columnName] = value;
            }
            
            return result;
        }
        
        return null;
    }

    /// <inheritdoc/>
    public async Task ApplyChanges(
        Key key,
        IChangeset<AppendedEvent, ExpandoObject> changeset,
        EventSequenceNumber eventSequenceNumber)
    {
        if (changeset.HasBeenRemoved())
        {
            await DeleteRecord(key);
            return;
        }

        var changes = await changesetConverter.ConvertToSqlOperations(key, changeset, eventSequenceNumber);
        
        if (!changes.HasChanges)
            return;

        await ExecuteSqlOperations(changes);
    }

    /// <inheritdoc/>
    public Task PrepareInitialRun()
    {
        return EnsureTableExists();
    }

    /// <inheritdoc/>
    public Task BeginReplay(Chronicle.Storage.Sinks.ReplayContext context)
    {
        // For SQL, we might want to disable constraints or create a shadow table
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ResumeReplay(Chronicle.Storage.Sinks.ReplayContext context)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task EndReplay(Chronicle.Storage.Sinks.ReplayContext context)
    {
        // Re-enable constraints or merge shadow table
        return Task.CompletedTask;
    }

    async Task EnsureTableExists()
    {
        var tableName = GetTableName();
        var createTableSql = GenerateCreateTableSql();
        
        await dbContext.Database.ExecuteSqlRawAsync(createTableSql);
    }

    async Task DeleteRecord(Key key)
    {
        var tableName = GetTableName();
        var keyColumnName = GetKeyColumnName();
        var sql = $"DELETE FROM {tableName} WHERE {keyColumnName} = @key";
        
        await dbContext.Database.ExecuteSqlRawAsync(sql, key.Value);
    }

    async Task ExecuteSqlOperations(SqlOperations operations)
    {
        foreach (var operation in operations.Operations)
        {
            await dbContext.Database.ExecuteSqlRawAsync(operation.Sql, operation.Parameters);
        }
    }

    string GetTableName()
    {
        // In a real implementation, this would be configurable and support namespacing
        return $"projection_{model.Name.Value.Replace("-", "_").Replace(".", "_")}";
    }

    string GetKeyColumnName()
    {
        // This could be configurable based on the model schema
        return "Id";
    }

    string GenerateCreateTableSql()
    {
        // This is a simplified implementation - in practice we'd analyze the JSON schema
        // and generate appropriate column definitions
        var tableName = GetTableName();
        var keyColumnName = GetKeyColumnName();
        
        return $@"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{tableName}' AND xtype='U')
            CREATE TABLE {tableName} (
                {keyColumnName} NVARCHAR(255) PRIMARY KEY,
                Data NVARCHAR(MAX),
                LastUpdated DATETIME2 DEFAULT GETUTCDATE()
            )";
    }
}