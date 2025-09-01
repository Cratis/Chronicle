// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Storage types supported by Chronicle SQL.
/// </summary>
public static class StorageType
{
    /// <summary>
    /// SQLite storage type.
    /// </summary>
    public const string Sqlite = "sqlite";

    /// <summary>
    /// SQL Server storage type.
    /// </summary>
    public const string SqlServer = "sqlserver";

    /// <summary>
    /// PostgreSQL storage type.
    /// </summary>
    public const string PostgreSql = "postgresql";
}
