// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator;

public class when_upgrading_a_sequence_with_legacy_revisions : when_upgrading_with_legacy_revisions, IDisposable
{
    SqliteConnection? _keepAlive;

    public void Dispose()
    {
        _keepAlive?.Dispose();
        GC.SuppressFinalize(this);
    }

    protected override async Task<string> CreateDatabase()
    {
        var connectionString = $"DataSource=legacy_revisions_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        _keepAlive = new SqliteConnection(connectionString);
        await _keepAlive.OpenAsync();
        return connectionString;
    }

    protected override void UseProvider(DbContextOptionsBuilder<EventSequenceDbContext> builder, string connectionString) =>
        builder.UseSqlite(connectionString);
}
