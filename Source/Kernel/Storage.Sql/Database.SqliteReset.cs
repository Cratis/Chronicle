// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEVELOPMENT
using Microsoft.Data.Sqlite;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// The part of <see cref="Database"/> that coordinates SQLite resets with migrations.
/// </summary>
public partial class Database
{
    async Task WipeAllSqliteDatabases(string clusterConnectionString)
    {
        var clusterPath = ExtractSqliteDataSource(clusterConnectionString);
        if (string.IsNullOrEmpty(clusterPath))
        {
            return;
        }

        var directory = Path.GetDirectoryName(clusterPath);
        if (string.IsNullOrEmpty(directory))
        {
            directory = ".";
        }

        var baseName = Path.GetFileNameWithoutExtension(clusterPath);
        var extension = Path.GetExtension(clusterPath);

        // Sidecar files aren't database files themselves; SQLite manages them with the main file.
        var pattern = string.IsNullOrEmpty(extension) ? $"{baseName}*" : $"{baseName}*{extension}";
        foreach (var file in Directory.GetFiles(directory, pattern))
        {
            if (file.EndsWith("-wal", StringComparison.Ordinal)
                || file.EndsWith("-shm", StringComparison.Ordinal)
                || file.EndsWith("-journal", StringComparison.Ordinal))
            {
                continue;
            }

            var connectionString = new SqliteConnectionStringBuilder { DataSource = file, Pooling = false }.ConnectionString;
            var migrationLock = DatabaseMigrationLock.For(connectionString);
            await migrationLock.Semaphore.WaitAsync();
            try
            {
                await DropAllSqliteTables(file);
                migrationLock.Generation++;
                ClearTableMigrationCache(string.Empty);
            }
            finally
            {
                migrationLock.Semaphore.Release();
            }
        }
    }
}
#endif
