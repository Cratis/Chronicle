// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_Database.given;

public static class migration_race
{
    public static async Task<(string[][] Pending, int Marker)> Run(Func<Database> create, DbConnection connection)
    {
        await using var ownedConnection = connection;
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE untouched_by_migrations (marker int NOT NULL)";
        await command.ExecuteNonQueryAsync();
        command.CommandText = "INSERT INTO untouched_by_migrations (marker) VALUES (42)";
        await command.ExecuteNonQueryAsync();

        var pending = await Task.WhenAll(Enumerable.Range(0, 4).Select(async _ =>
        {
            await using var scope = await create().Cluster();
            return (await scope.DbContext.Database.GetPendingMigrationsAsync()).ToArray();
        })).WaitAsync(TimeSpan.FromSeconds(30));

        command.CommandText = "SELECT marker FROM untouched_by_migrations";
        return (pending, Convert.ToInt32(await command.ExecuteScalarAsync()));
    }
}
