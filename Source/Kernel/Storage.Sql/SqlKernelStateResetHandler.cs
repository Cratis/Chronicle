// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Releases pooled SQL connections when the kernel is being reset between integration test specs.
/// Each provider keeps its own pool, so the harness must clear them before deleting or truncating
/// the underlying database; otherwise the next open reuses a stale handle.
/// </summary>
/// <param name="options"><see cref="IOptions{ChronicleOptions}"/> describing the active storage backend.</param>
public class SqlKernelStateResetHandler(IOptions<ChronicleOptions> options) : IPerformKernelStateReset
{
    /// <inheritdoc/>
    public Task Reset()
    {
        var storageType = options.Value.Storage.Type;
        if (string.Equals(storageType, StorageType.Sqlite, StringComparison.OrdinalIgnoreCase))
        {
            SqliteConnection.ClearAllPools();
        }
        else if (string.Equals(storageType, StorageType.PostgreSql, StringComparison.OrdinalIgnoreCase))
        {
            NpgsqlConnection.ClearAllPools();
        }
        else if (string.Equals(storageType, StorageType.MsSql, StringComparison.OrdinalIgnoreCase))
        {
            SqlConnection.ClearAllPools();
        }

        return Task.CompletedTask;
    }
}
