// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.SQL;

/// <summary>
/// Supported SQL provider types.
/// </summary>
public enum SqlProviderType
{
    /// <summary>
    /// Microsoft SQL Server.
    /// </summary>
    SqlServer = 1,

    /// <summary>
    /// PostgreSQL.
    /// </summary>
    PostgreSQL = 2,

    /// <summary>
    /// SQLite.
    /// </summary>
    SQLite = 3
}
