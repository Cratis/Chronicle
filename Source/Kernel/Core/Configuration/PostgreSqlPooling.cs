// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents PostgreSQL connection pool settings.
/// </summary>
/// <remarks>
/// The kernel keeps one pool per database, so the maximum connection count is roughly the number of
/// databases multiplied by <see cref="MaxPoolSize"/>. Values in the connection string take precedence.
/// </remarks>
public class PostgreSqlPooling
{
    /// <summary>
    /// Gets the maximum number of connections per database pool. Defaults to 20.
    /// </summary>
    public int MaxPoolSize { get; init; } = 20;

    /// <summary>
    /// Gets how many seconds a connection can remain idle before being closed when the pool exceeds its minimum size. Defaults to 15.
    /// </summary>
    public int ConnectionIdleLifetimeSeconds { get; init; } = 15;

    /// <summary>
    /// Gets how often, in seconds, the pool samples idle connections for pruning. Defaults to 5.
    /// </summary>
    /// <remarks>
    /// The interval must be greater than zero and no greater than the idle lifetime.
    /// </remarks>
    public int ConnectionPruningIntervalSeconds { get; init; } = 5;
}
