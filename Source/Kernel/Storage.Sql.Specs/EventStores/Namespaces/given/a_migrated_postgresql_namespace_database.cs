// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.given;

/// <summary>
/// Sets up a PostgreSQL database of its own whose schema comes from the namespace migrations the kernel
/// applies. PostgreSQL stores timestamps with microsecond precision, which SQLite does not.
/// </summary>
/// <param name="fixture">The <see cref="PostgreSqlFixture"/> supplying the container.</param>
public abstract class a_migrated_postgresql_namespace_database(PostgreSqlFixture fixture) : Specification
{
    protected static readonly EventStoreName _eventStore = "test-store";
    protected static readonly EventStoreNamespaceName _namespace = "test-namespace";
    protected IDatabase _database;
    string _connectionString;

    async Task Establish()
    {
        _connectionString = await fixture.CreateDatabase();

        await using (var migrationContext = CreateContext())
        {
            await migrationContext.Database.MigrateAsync();
        }

        _database = Substitute.For<IDatabase>();
        _database.Namespace(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(_ => new DbContextScope<NamespaceDbContext>(CreateContext(), () => { }));
    }

    Task Destroy() => fixture.DropDatabase(_connectionString);

    NamespaceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NamespaceDbContext>()
            .UseNpgsql(_connectionString)
            .AddConceptAsSupport()
            .Options;

        return new NamespaceDbContext(options);
    }
}
