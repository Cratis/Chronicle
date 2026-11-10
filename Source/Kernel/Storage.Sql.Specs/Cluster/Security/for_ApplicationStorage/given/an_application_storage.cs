// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Application = Cratis.Chronicle.Storage.Security.Application;
using ApplicationId = Cratis.Chronicle.Concepts.Security.ApplicationId;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Security.for_ApplicationStorage.given;

/// <summary>
/// Sets up a SQL <see cref="ApplicationStorage"/> backed by an in-memory SQLite cluster database.
/// </summary>
public class an_application_storage : Specification
{
    protected SqliteConnection _connection;
    protected IDatabase _database;
    protected ApplicationStorage _storage;
    protected ApplicationId _id;

    void Establish()
    {
        _id = Guid.NewGuid();
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        using (var context = CreateContext())
        {
            context.Database.EnsureCreated();
        }

        _database = Substitute.For<IDatabase>();
        _database.Cluster().Returns(_ => Task.FromResult(new DbContextScope<ClusterDbContext>(CreateContext(), () => { })));
        _storage = new ApplicationStorage(_database);
    }

    void Destroy()
    {
        _storage.Dispose();
        _connection.Dispose();
    }

    protected Application CreateApplication(string clientId, string secret) => new()
    {
        Id = _id,
        ClientId = (ClientId)clientId,
        ClientSecret = (ClientSecret)secret
    };

    ClusterDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ClusterDbContext>()
            .UseSqlite(_connection)
            .AddConceptAsSupport().WithoutServiceProviderCaching()
            .Options;
        return new ClusterDbContext(options);
    }
}
