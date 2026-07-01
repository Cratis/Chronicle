// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage;

/// <summary>
/// Storage backend types supported by Chronicle.
/// </summary>
public static class StorageType
{
    /// <summary>
    /// MongoDB storage type. Also the default when <c>Storage.Type</c> is unspecified.
    /// </summary>
    public const string MongoDB = "mongodb";

    /// <summary>
    /// In-memory storage type. All state is kept in process memory and lost on exit — intended for
    /// tests, samples, and ephemeral environments rather than durable production use.
    /// </summary>
    public const string InMemory = "inmemory";

    /// <summary>
    /// SQLite storage type.
    /// </summary>
    public const string Sqlite = "sqlite";

    /// <summary>
    /// Microsoft SQL Server storage type.
    /// </summary>
    public const string MsSql = "mssql";

    /// <summary>
    /// PostgreSQL storage type.
    /// </summary>
    public const string PostgreSql = "postgresql";
}
