// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Security.for_UserStorage.given;

/// <summary>
/// Sets up a SQL <see cref="UserStorage"/> backed by an in-memory SQLite cluster database that keeps
/// a single connection open so every <see cref="IDatabase.Cluster"/> scope sees the same data. A user
/// is seeded with a deliberately lower-cased username and email so the specs can prove case-insensitive
/// lookups, matching the InMemory and MongoDB backends.
/// </summary>
public class a_user_storage : Specification
{
    protected SqliteConnection _connection;
    protected IDatabase _database;
    protected UserStorage _storage;
    protected UserId _userId;
    protected Username _storedUsername;
    protected UserEmail _storedEmail;

    void Establish()
    {
        _userId = Guid.NewGuid();
        _storedUsername = "admin";
        _storedEmail = "admin@cratis.io";

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using (var context = CreateContext())
        {
            context.Database.EnsureCreated();
            context.Users.Add(new UserEntity
            {
                Id = _userId.Value,
                Username = _storedUsername.Value,
                Email = _storedEmail.Value,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            });
            context.SaveChanges();
        }

        _database = Substitute.For<IDatabase>();
        _database.Cluster().Returns(_ => Task.FromResult(new DbContextScope<ClusterDbContext>(CreateContext(), () => { })));

        _storage = new UserStorage(_database);
    }

    void Destroy()
    {
        _storage.Dispose();
        _connection.Dispose();
    }

    ClusterDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ClusterDbContext>()
            .UseSqlite(_connection)
            .AddConceptAsSupport()
            .Options;

        return new ClusterDbContext(options);
    }
}
