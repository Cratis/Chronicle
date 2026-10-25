// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Storage.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Security.for_AdministratorStorage.given;

public class an_administrator_storage : Specification
{
    protected SqliteConnection _connection;
    protected IDatabase _database;
    protected AdministratorStorage _storage;
    protected Administrator _grant;

    void Establish()
    {
        _grant = new Administrator(Guid.NewGuid(), GrantReason.OperatorRecovery, "audit evidence");
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        using (var context = CreateContext()) context.Database.EnsureCreated();
        _database = Substitute.For<IDatabase>();
        _database.Cluster().Returns(_ => Task.FromResult(new DbContextScope<ClusterDbContext>(CreateContext(), () => { })));
        _storage = new AdministratorStorage(_database);
    }

    void Destroy() => _connection.Dispose();

    protected ClusterDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ClusterDbContext>()
            .UseSqlite(_connection).AddConceptAsSupport().WithoutServiceProviderCaching().Options;
        return new ClusterDbContext(options);
    }
}
