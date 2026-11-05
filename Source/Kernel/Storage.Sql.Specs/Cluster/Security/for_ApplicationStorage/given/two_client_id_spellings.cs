// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Security;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CA2100 // Fixture-owned values only.

namespace Cratis.Chronicle.Storage.Sql.Cluster.Security.for_ApplicationStorage.given;

/// <summary>
/// Two spellings of a client id that the default collation of SQL Server treats as one.
/// </summary>
/// <param name="fixture">The SQL Server fixture.</param>
/// <param name="first">The first spelling.</param>
/// <param name="second">The second spelling.</param>
public abstract class two_client_id_spellings(SqlServerFixture fixture, string first, string second) : Specification
{
    protected string _first = first;
    protected string _second = second;
    protected long _collapsedByCollation;
    protected Application? _foundForFirst;
    protected Application? _foundForSecond;
    string _connectionString = string.Empty;

    protected async Task SaveBoth()
    {
        _connectionString = await fixture.CreateDatabase();
        var options = new DbContextOptionsBuilder<ClusterDbContext>().UseSqlServer(_connectionString).AddConceptAsSupport().WithoutServiceProviderCaching().Options;
        await using (var context = new ClusterDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = $"SELECT CASE WHEN N'{_first}' = N'{_second}' THEN 1 ELSE 0 END";
            await context.Database.OpenConnectionAsync();
            _collapsedByCollation = Convert.ToInt64(await command.ExecuteScalarAsync());
        }

        var database = Substitute.For<IDatabase>();
        database.Cluster().Returns(_ => Task.FromResult(new DbContextScope<ClusterDbContext>(new(options), () => { })));
        using var storage = new ApplicationStorage(database);
        await storage.Create(new Application { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), ClientId = new(_first) });
        _foundForFirst = await storage.GetByClientId(new(_first));
        _foundForSecond = await storage.GetByClientId(new(_second));
    }

    protected void Destroy() => fixture.DropDatabase(_connectionString).GetAwaiter().GetResult();
}
