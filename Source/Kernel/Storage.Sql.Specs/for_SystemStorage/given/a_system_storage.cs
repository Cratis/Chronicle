// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_SystemStorage.given;

public class a_system_storage : Specification
{
    protected SystemStorage _storage;
    SqliteConnection _connection;
    DbContextOptions<ClusterDbContext> _options;

    async Task Establish()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        _options = new DbContextOptionsBuilder<ClusterDbContext>()
            .UseSqlite(_connection).AddConceptAsSupport().WithoutServiceProviderCaching().Options;
        await using var context = new ClusterDbContext(_options);
        await context.Database.EnsureCreatedAsync();
        var database = Substitute.For<IDatabase>();
        database.Cluster().Returns(_ => Task.FromResult(new DbContextScope<ClusterDbContext>(new ClusterDbContext(_options), () => { })));
        _storage = new SystemStorage(database);
    }

    void Destroy() => _connection.Dispose();
}
