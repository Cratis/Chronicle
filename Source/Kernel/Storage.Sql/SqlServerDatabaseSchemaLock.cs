// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Represents an <see cref="IDatabaseSchemaLock"/> for SQL Server, using an exclusive session-owned application lock.
/// </summary>
internal sealed class SqlServerDatabaseSchemaLock : IDatabaseSchemaLock
{
    /// <summary>
    /// Gets the shared instance.
    /// </summary>
    internal static readonly SqlServerDatabaseSchemaLock Instance = new();

    /// <summary>
    /// How much longer the command may run than the lock waits, so the lock timeout is the one that fires.
    /// </summary>
    const int CommandTimeoutMarginSeconds = 30;

    /// <inheritdoc/>
    public async Task Acquire(DbConnection connection, string resource, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            "DECLARE @result int; " +
            "EXEC @result = sp_getapplock @Resource = @resource, @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = @timeout; " +
            "SELECT @result;";
        command.CommandTimeout = (int)Math.Ceiling(timeout.TotalSeconds) + CommandTimeoutMarginSeconds;
        AddParameter(command, "resource", resource);
        AddParameter(command, "timeout", (int)timeout.TotalMilliseconds);
        var result = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));

        // 0 granted at once, 1 granted after waiting, -1 timed out, -2 canceled, -3 deadlock victim, -999 error.
        if (result < 0)
        {
            throw new DatabaseSchemaLockNotAcquired(connection.Database, resource, result == -1 ? $"timed out after {timeout}" : $"sp_getapplock returned {result}");
        }
    }

    /// <inheritdoc/>
    public async Task Release(DbConnection connection, string resource)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "EXEC sp_releaseapplock @Resource = @resource, @LockOwner = 'Session';";
        AddParameter(command, "resource", resource);
        await command.ExecuteNonQueryAsync();
    }

    static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
