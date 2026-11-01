// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.Sql.for_Database.given;

/// <summary>
/// A SQL Server whose event store list names a store, and a principal that is not sysadmin but owns the specification database.
/// The store's database is either OFFLINE, which only a principal with the right permissions can see, or missing.
/// </summary>
/// <param name="fixture">The <see cref="SqlServerFixture"/> owning the server.</param>
/// <param name="grant">The server or master statement that gives the principal its permission, or empty for none.</param>
/// <param name="offline">Whether the store's database exists and is OFFLINE.</param>
public class a_sql_server_principal_with_hidden_databases(SqlServerFixture fixture, string grant, bool offline) : a_sql_server_database(fixture)
{
    const string Login = "chronicle_limited";
    const string Password = "Limited4476!Test";
    const string Store = "legacy-store";

    private protected static readonly ProjectedColumn[] _columns = [new("id", typeof(string), true, false, false, false)];

    private protected readonly ReadModelTableHandle _ready = ReadModelTableHandle.For("a-store", "default", "customers", "customers", ReadModelTableRole.Live, "customers");
    private protected readonly ReadModelTableHandle _new = ReadModelTableHandle.For("a-store", "default", "orders", "orders", ReadModelTableRole.Live, "orders");
    private protected Database _limited;
    string _legacyDatabase;

    async Task Establish()
    {
        // A Ready binding exists before the principal loses visibility.
        await using (var scope = await Database.Tables.Open(_ready, _columns))
        {
            (await scope.DbContext.Entries.CountAsync()).ShouldEqual(0);
        }

        await using (var cluster = await Database.Cluster())
        {
            cluster.DbContext.EventStores.Add(new() { Name = Store });
            await cluster.DbContext.SaveChangesAsync();
        }

        var specDatabase = new SqlConnectionStringBuilder(ConnectionString).InitialCatalog;
        _legacyDatabase = $"{specDatabase}+es+{Store}";
        if (offline)
        {
            await Execute("master", $"CREATE DATABASE [{_legacyDatabase}]");
            await Execute("master", $"ALTER DATABASE [{_legacyDatabase}] SET OFFLINE WITH ROLLBACK IMMEDIATE");
        }

        await Execute("master", $"CREATE LOGIN [{Login}] WITH PASSWORD = '{Password}', CHECK_POLICY = OFF");
        await Execute(specDatabase, $"CREATE USER [{Login}] FOR LOGIN [{Login}]; ALTER ROLE db_owner ADD MEMBER [{Login}];");
        await using (var cluster = await Database.Cluster())
        {
            foreach (var name in await cluster.DbContext.DatabaseNames.Select(row => row.PhysicalName).Distinct().ToArrayAsync())
            {
                if (name != specDatabase)
                {
                    await Execute(name, $"CREATE USER [{Login}] FOR LOGIN [{Login}]; ALTER ROLE db_owner ADD MEMBER [{Login}];");
                }
            }
        }
        if (grant.Length > 0)
        {
            await Execute("master", grant.Replace("{login}", Login, StringComparison.Ordinal));
        }

        var limited = new SqlConnectionStringBuilder(ConnectionString) { UserID = Login, Password = Password, Pooling = false }.ConnectionString;
        _limited = new(
            Services,
            Options.Create(new ChronicleOptions { Storage = new() { Type = StorageType.MsSql, ConnectionDetails = limited } }),
            Substitute.For<IEventSequenceMigrator>(),
            Substitute.For<IUniqueConstraintMigrator>(),
            Services.GetRequiredService<IReadModelMigrator>());
    }

    async Task Destroy()
    {
        SqlConnection.ClearAllPools();
        if (offline && _legacyDatabase is not null)
        {
            await Fixture.DropDatabase(new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = _legacyDatabase }.ConnectionString);
        }

        await Execute("master", $"IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '{Login}') DROP USER [{Login}]; IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '{Login}') DROP LOGIN [{Login}]");
    }

    async Task Execute(string database, string sql)
    {
        await using var connection = new SqlConnection(new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = database }.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
#pragma warning disable CA2100 // Spec-owned identifiers and constants only.
        command.CommandText = sql;
#pragma warning restore CA2100
        await command.ExecuteNonQueryAsync();
    }
}
