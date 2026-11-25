// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Storage.Sql.Connections;

/// <summary>
/// Captures every input that can change a normalized connection string, including credentials in either string.
/// </summary>
/// <param name="ConnectionString">The configured or derived connection string.</param>
/// <param name="Schema">The effective schema.</param>
/// <param name="MaxPoolSize">The configured maximum pool size.</param>
/// <param name="IdleLifetime">The configured idle lifetime.</param>
/// <param name="PruningInterval">The configured pruning interval.</param>
/// <param name="PlacementConnectionString">The resolved connection string, when placement is applied.</param>
/// <param name="Database">The resolved database, when placement is applied.</param>
internal readonly record struct SqlConnectionStringInput(string ConnectionString, string Schema, int MaxPoolSize, int IdleLifetime, int PruningInterval, string? PlacementConnectionString, string? Database)
{
    /// <summary>
    /// Snapshot the string-affecting settings without retaining resolved identities or mutable options.
    /// </summary>
    /// <param name="connectionString">The configured or derived connection string.</param>
    /// <param name="schema">The effective schema.</param>
    /// <param name="pooling">The configured pooling settings.</param>
    /// <param name="placement">The resolved placement, if it is to be applied.</param>
    /// <returns>The normalization input.</returns>
    internal static SqlConnectionStringInput From(string connectionString, string schema, PostgreSqlPooling pooling, ResolvedConnection? placement = null) =>
        new(connectionString, schema, pooling.MaxPoolSize, pooling.ConnectionIdleLifetimeSeconds, pooling.ConnectionPruningIntervalSeconds, placement?.ConnectionString, placement?.Database);

    /// <summary>
    /// Apply the existing placement, schema and pooling transformations, in their original order.
    /// </summary>
    /// <returns>The normalized connection string.</returns>
    internal string Normalize()
    {
        var placed = SqlConnectionStrings.WithPlacement(ConnectionString, new(PlacementConnectionString, Database));
        return SqlConnectionStrings.WithPooling(SqlConnectionStrings.WithSchema(placed, Schema), new()
        {
            MaxPoolSize = MaxPoolSize,
            ConnectionIdleLifetimeSeconds = IdleLifetime,
            ConnectionPruningIntervalSeconds = PruningInterval
        });
    }
}
