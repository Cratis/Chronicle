// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Represents an <see cref="IDatabaseSchemaLock"/> for PostgreSQL, using a session-level advisory lock.
/// </summary>
/// <remarks>
/// Advisory locks are keyed by a number and scoped to the current database, so the key is a stable hash of the
/// resource name. Acquisition polls <c language="sql">pg_try_advisory_lock</c>, which keeps the wait bounded by the timeout
/// without depending on the session's <c language="sql">lock_timeout</c>.
/// </remarks>
internal sealed class PostgreSqlDatabaseSchemaLock : IDatabaseSchemaLock
{
    /// <summary>
    /// Gets the shared instance.
    /// </summary>
    internal static readonly PostgreSqlDatabaseSchemaLock Instance = new();

    static readonly TimeSpan _firstRetryDelay = TimeSpan.FromMilliseconds(50);
    static readonly TimeSpan _maxRetryDelay = TimeSpan.FromSeconds(1);

    /// <inheritdoc/>
    public async Task Acquire(DbConnection connection, string resource, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var key = KeyFor(resource);
        var deadline = DateTimeOffset.UtcNow + timeout;
        var delay = _firstRetryDelay;
        while (!await Execute(connection, "SELECT pg_try_advisory_lock(@key)", key, cancellationToken))
        {
            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new DatabaseSchemaLockNotAcquired(connection.Database, resource, $"timed out after {timeout}");
            }

            await Task.Delay(delay, cancellationToken);
            delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, _maxRetryDelay.Ticks));
        }
    }

    /// <inheritdoc/>
    public Task Release(DbConnection connection, string resource) =>
        Execute(connection, "SELECT pg_advisory_unlock(@key)", KeyFor(resource), CancellationToken.None);

    /// <summary>
    /// Get the advisory lock key for a resource name.
    /// </summary>
    /// <param name="resource">The name of what is locked.</param>
    /// <returns>The key, the same in every process.</returns>
    internal static long KeyFor(string resource) => BinaryPrimitives.ReadInt64BigEndian(SHA256.HashData(Encoding.UTF8.GetBytes(resource)));

    static async Task<bool> Execute(DbConnection connection, string sql, long key, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
#pragma warning disable CA2100 // Constant SQL; the key is a parameter.
        command.CommandText = sql;
#pragma warning restore CA2100
        var parameter = command.CreateParameter();
        parameter.ParameterName = "key";
        parameter.Value = key;
        command.Parameters.Add(parameter);
        return Convert.ToBoolean(await command.ExecuteScalarAsync(cancellationToken));
    }
}
