// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Concepts.Models;
using NJsonSchema;

namespace Cratis.Chronicle.Storage.SQL.Models;

/// <summary>
/// Generates SQL DDL statements from JSON schemas.
/// </summary>
public class SqlSchemaGenerator
{
    readonly SqlProviderType _providerType;
    readonly string _schema;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlSchemaGenerator"/> class.
    /// </summary>
    /// <param name="providerType">The SQL provider type.</param>
    /// <param name="schema">The schema name.</param>
    public SqlSchemaGenerator(SqlProviderType providerType, string schema)
    {
        _providerType = providerType;
        _schema = schema;
    }

    /// <summary>
    /// Generate CREATE TABLE SQL for a model.
    /// </summary>
    /// <param name="model">The model to generate SQL for.</param>
    /// <returns>SQL DDL statement.</returns>
    public string GenerateCreateTableSql(Model model)
    {
        var tableName = GetTableName(model.Name);
        var columns = GenerateColumns(model.Schema);

        var sql = new StringBuilder();

        if (_providerType == SqlProviderType.SqlServer)
        {
            sql
                .AppendLine($@"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '{tableName}' AND schema_id = SCHEMA_ID('{_schema}'))")
                .AppendLine("BEGIN")
                .AppendLine($"CREATE TABLE [{_schema}].[{tableName}] (");
        }
        else if (_providerType == SqlProviderType.PostgreSQL)
        {
            sql.AppendLine($@"CREATE TABLE IF NOT EXISTS ""{_schema}"".""{tableName}"" (");
        }
        else // SQLite
        {
            sql.AppendLine($@"CREATE TABLE IF NOT EXISTS ""{tableName}"" (");
        }

        // SQLite doesn't support NVARCHAR, use TEXT instead
        var idType = _providerType == SqlProviderType.SQLite ? "TEXT" : "NVARCHAR(255)";
        var timestampDefault = _providerType switch
        {
            SqlProviderType.SqlServer => "DEFAULT GETUTCDATE()",
            SqlProviderType.PostgreSQL => "DEFAULT CURRENT_TIMESTAMP",
            SqlProviderType.SQLite => "DEFAULT (datetime('now'))",
            _ => "DEFAULT CURRENT_TIMESTAMP"
        };

        sql
            .AppendLine($"    Id {idType} PRIMARY KEY,")
            .AppendLine("    EventSequenceNumber BIGINT NOT NULL,")
            .AppendLine($"    LastUpdated TIMESTAMP {timestampDefault},");

        foreach (var column in columns)
        {
            sql.AppendLine($"    {column},");
        }

        // SQLite doesn't support NVARCHAR(MAX), use TEXT instead
        var dataType = _providerType == SqlProviderType.SQLite ? "TEXT" : "NVARCHAR(MAX)";
        sql
            .AppendLine($"    Data {dataType}") // Fallback JSON storage
            .AppendLine(")");

        if (_providerType == SqlProviderType.SqlServer)
        {
            sql.AppendLine("END");
        }

        return sql.ToString();
    }

    /// <summary>
    /// Generate column definitions from JSON schema.
    /// </summary>
    /// <param name="schema">The JSON schema.</param>
    /// <returns>Column definitions.</returns>
    public IEnumerable<string> GenerateColumns(JsonSchema schema)
    {
        var columns = new List<string>();

        foreach (var property in schema.Properties)
        {
            var columnName = GetColumnName(property.Key);
            var columnType = GetSqlType(property.Value);

            if (columnType != null)
            {
                var identifier = _providerType == SqlProviderType.SQLite ? $"\"{columnName}\"" : $"[{columnName}]";
                columns.Add($"{identifier} {columnType}");
            }
        }

        return columns;
    }

    string? GetSqlType(JsonSchema propertySchema)
    {
        return propertySchema.Type switch
        {
            JsonObjectType.String => _providerType switch
            {
                SqlProviderType.SqlServer => "NVARCHAR(450)",
                SqlProviderType.PostgreSQL => "VARCHAR(450)",
                SqlProviderType.SQLite => "TEXT",
                _ => "VARCHAR(450)"
            },
            JsonObjectType.Integer => "BIGINT",
            JsonObjectType.Number => _providerType == SqlProviderType.SQLite ? "REAL" : "DECIMAL(18,6)",
            JsonObjectType.Boolean => _providerType switch
            {
                SqlProviderType.SqlServer => "BIT",
                SqlProviderType.PostgreSQL => "BOOLEAN",
                SqlProviderType.SQLite => "INTEGER",
                _ => "BOOLEAN"
            },
            JsonObjectType.Object => null, // Store as JSON in main Data column
            JsonObjectType.Array => null, // Store as JSON in main Data column
            _ => null
        };
    }

    string GetTableName(ModelName modelName)
    {
        return $"projection_{modelName.Value.Replace('-', '_').Replace('.', '_')}";
    }

    string GetColumnName(string propertyName)
    {
        // Convert camelCase to snake_case for database consistency
        return propertyName;
    }
}
