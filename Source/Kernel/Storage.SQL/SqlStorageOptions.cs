// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.SQL;

/// <summary>
/// Configuration options for SQL storage.
/// </summary>
public class SqlStorageOptions
{
    /// <summary>
    /// Gets or sets the database provider type.
    /// </summary>
    public SqlProviderType ProviderType { get; set; } = SqlProviderType.SqlServer;

    /// <summary>
    /// Gets or sets the connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schema name for SQL Server or namespace strategy for PostgreSQL.
    /// </summary>
    public string Schema { get; set; } = "dbo";

    /// <summary>
    /// Gets or sets a value indicating whether to use schemas for namespacing.
    /// </summary>
    public bool UseSchemaForNamespacing { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to create schema/database automatically.
    /// </summary>
    public bool AutoCreateSchema { get; set; } = true;
}
