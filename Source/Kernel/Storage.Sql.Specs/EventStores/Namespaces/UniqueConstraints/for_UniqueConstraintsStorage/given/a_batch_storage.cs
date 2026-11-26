// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.given;

public class a_batch_storage : a_provider_batch_storage
{
    SqliteConnection _connection;

    async Task Establish()
    {
        _connection = new SqliteConnection($"DataSource=batch-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        await _connection.OpenAsync();
        Configure(builder => builder.UseSqlite(_connection.ConnectionString), false, () => Task.CompletedTask);
    }

    Task Destroy() => _connection.DisposeAsync().AsTask();
}
