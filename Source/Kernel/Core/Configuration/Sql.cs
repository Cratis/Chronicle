// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the configuration for the SQL storage backends.
/// </summary>
public class Sql
{
    /// <summary>
    /// Gets the interval in seconds between polls of the database for live, database-watching queries.
    /// </summary>
    /// <remarks>
    /// The SQL backends cannot use push-based change streams the way MongoDB does, so live queries
    /// poll the database on this interval and re-emit only when the result actually changes.
    /// </remarks>
    public int LiveQueryPollIntervalSeconds { get; init; } = 2;

    /// <summary>
    /// Gets the finite timeout in seconds for shadow-index CREATE and DROP statements and their reconciliation deadline.
    /// Must be positive. The default is ten minutes; ordinary SQL commands retain their own timeout.
    /// </summary>
    public int ShadowIndexDdlTimeoutSeconds { get; init; } = 600;
}
