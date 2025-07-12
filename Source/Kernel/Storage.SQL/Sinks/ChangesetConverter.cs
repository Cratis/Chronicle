// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Models;
using Cratis.Chronicle.Json;

namespace Cratis.Chronicle.Storage.SQL.Sinks;

/// <summary>
/// Represents an implementation of <see cref="IChangesetConverter"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ChangesetConverter"/> class.
/// </remarks>
/// <param name="model">The <see cref="Model"/> the converter is for.</param>
/// <param name="expandoObjectConverter">The <see cref="IExpandoObjectConverter"/> for converting objects.</param>
public class ChangesetConverter(
    Model model,
    IExpandoObjectConverter expandoObjectConverter) : IChangesetConverter
{
    /// <inheritdoc/>
    public Task<SqlOperations> ConvertToSqlOperations(
        Key key,
        IChangeset<AppendedEvent, ExpandoObject> changeset,
        EventSequenceNumber eventSequenceNumber)
    {
        var operations = new List<SqlOperation>();

        if (changeset.HasJoined())
        {
            // Handle initial creation
            var insertSql = GenerateInsertSql(key, changeset.InitialState);
            operations.Add(insertSql);
        }
        else
        {
            // Handle updates
            foreach (var change in changeset.Changes)
            {
                var updateSql = GenerateUpdateSqlForChange(key, change);
                if (updateSql != null)
                {
                    operations.Add(updateSql);
                }
            }
        }

        return Task.FromResult(new SqlOperations(operations));
    }

    SqlOperation GenerateInsertSql(Key key, ExpandoObject state)
    {
        var tableName = GetTableName();
        var keyColumnName = GetKeyColumnName();

        // For now, we'll serialize the entire state as JSON
        // In a more sophisticated implementation, we'd map individual properties to columns
        var jsonData = System.Text.Json.JsonSerializer.Serialize(state);

        var sql = @"
            INSERT INTO {0} ({1}, Data, LastUpdated)
            VALUES (@key, @data, @lastUpdated)".Replace("{0}", tableName).Replace("{1}", keyColumnName);

        return SqlOperation.Create(sql, key.Value, jsonData, DateTime.UtcNow);
    }

    SqlOperation? GenerateUpdateSqlForChange(Key key, Change change)
    {
        var tableName = GetTableName();
        var keyColumnName = GetKeyColumnName();

        return change switch
        {
            PropertiesChanged<ExpandoObject> propertiesChanged => GenerateUpdateForPropertiesChanged(key, propertiesChanged),
            ChildAdded childAdded => GenerateUpdateForChildAdded(key, childAdded),
            ChildRemoved childRemoved => GenerateUpdateForChildRemoved(key, childRemoved),
            _ => null
        };
    }

    SqlOperation GenerateUpdateForPropertiesChanged(Key key, PropertiesChanged<ExpandoObject> propertiesChanged)
    {
        var tableName = GetTableName();
        var keyColumnName = GetKeyColumnName();

        // For simplicity, we'll update the entire JSON blob
        // In practice, we might want to update specific columns based on the properties changed
        var jsonData = System.Text.Json.JsonSerializer.Serialize(propertiesChanged.State);

        var sql = @"
            UPDATE {0}
            SET Data = @data, LastUpdated = @lastUpdated
            WHERE {1} = @key".Replace("{0}", tableName).Replace("{1}", keyColumnName);

        return SqlOperation.Create(sql, jsonData, DateTime.UtcNow, key.Value);
    }

    SqlOperation GenerateUpdateForChildAdded(Key key, ChildAdded childAdded)
    {
        // This would require more sophisticated JSON manipulation
        // For now, we'll treat it as a general update
        var tableName = GetTableName();
        var keyColumnName = GetKeyColumnName();

        var sql = @"
            UPDATE {0}
            SET LastUpdated = @lastUpdated
            WHERE {1} = @key".Replace("{0}", tableName).Replace("{1}", keyColumnName);

        return SqlOperation.Create(sql, DateTime.UtcNow, key.Value);
    }

    SqlOperation GenerateUpdateForChildRemoved(Key key, ChildRemoved childRemoved)
    {
        // Similar to child added - would need JSON manipulation
        var tableName = GetTableName();
        var keyColumnName = GetKeyColumnName();

        var sql = @"
            UPDATE {0}
            SET LastUpdated = @lastUpdated
            WHERE {1} = @key".Replace("{0}", tableName).Replace("{1}", keyColumnName);

        return SqlOperation.Create(sql, DateTime.UtcNow, key.Value);
    }

    string GetTableName()
    {
        return $"projection_{model.Name.Value.Replace('-', '_').Replace('.', '_')}";
    }

    string GetKeyColumnName()
    {
        return "Id";
    }
}
