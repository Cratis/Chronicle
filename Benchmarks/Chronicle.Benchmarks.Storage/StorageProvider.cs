// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>The storage providers the harness can measure.</summary>
public enum StorageProvider
{
    /// <summary>MongoDB.</summary>
    Mongo = 0,

    /// <summary>PostgreSQL.</summary>
    PostgreSql = 1,

    /// <summary>SQL Server.</summary>
    SqlServer = 2,

    /// <summary>SQLite (file-backed).</summary>
    Sqlite = 3
}
