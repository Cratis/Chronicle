// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.given;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlSingleWriterLeaseStorage.given;

public sealed class LeaseHarness(MembershipHarness? server = null) : IAsyncDisposable
{
    SqliteConnection? _anchor;

    public async Task Initialize()
    {
        if (server is not null)
        {
            await server.Initialize(false);
            return;
        }

        _anchor = new SqliteConnection($"Data Source=lease_{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        await _anchor.OpenAsync();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public ClusterDbContext CreateContext() => server?.CreateContext() ?? new ClusterDbContext(
        new DbContextOptionsBuilder<ClusterDbContext>().UseSqlite(_anchor!.ConnectionString).AddConceptAsSupport().Options);

    public async Task Expire()
    {
        await using var context = CreateContext();
        await context.Set<SingleWriterLeaseRow>().ExecuteUpdateAsync(setters => setters.SetProperty(row => row.ExpiresAt, DateTime.UnixEpoch));
    }

    public async ValueTask DisposeAsync()
    {
        if (server is not null) await server.DisposeAsync();
        if (_anchor is not null) await _anchor.DisposeAsync();
    }
}
