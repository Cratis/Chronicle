// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.SQL.Sinks;

/// <summary>
/// Represents a single SQL operation.
/// </summary>
/// <param name="Sql">The SQL statement to execute.</param>
/// <param name="Parameters">Parameters for the SQL statement.</param>
public record SqlOperation(string Sql, IEnumerable<object> Parameters)
{
    /// <summary>
    /// Create a SQL operation with parameters.
    /// </summary>
    /// <param name="sql">The SQL statement.</param>
    /// <param name="parameters">Parameters for the SQL statement.</param>
    /// <returns>A new SQL operation.</returns>
    public static SqlOperation Create(string sql, params object[] parameters)
    {
        return new SqlOperation(sql, parameters);
    }
}