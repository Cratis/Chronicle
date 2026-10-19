// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Arc.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Represents a <see cref="IHealthCheck"/> that connects to the cluster database the way the storage does.
/// </summary>
/// <remarks>
/// The connection is prepared with the identity the storage connection resolution returns — password, access token or
/// managed identity — instead of relying on what the configured connection details alone carry.
/// </remarks>
/// <param name="connectionDetails">The configured connection string.</param>
/// <param name="prepareConnection">Prepares the closed connection with the resolved identity before it opens.</param>
internal sealed class SqlConnectionHealthCheck(string connectionDetails, Func<DbConnection, Task> prepareConnection) : IHealthCheck
{
    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = CreateConnection();
            await prepareConnection(connection);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1;";
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }

    DbConnection CreateConnection() => connectionDetails.GetDatabaseType() switch
    {
        DatabaseType.PostgreSql => new NpgsqlConnection(connectionDetails),
        _ => new SqlConnection(connectionDetails)
    };
}
