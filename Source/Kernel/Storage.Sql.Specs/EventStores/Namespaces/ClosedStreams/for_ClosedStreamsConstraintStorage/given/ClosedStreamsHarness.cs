// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsConstraintStorage.given;

public sealed class ClosedStreamsHarness(string provider, Func<Task<string>> createDatabase, Func<string, Task> dropDatabase) : IAsyncDisposable
{
    string _connectionString;

    public ClosedStreamsConstraintStorage Storage { get; private set; }

    public async Task Initialize()
    {
        _connectionString = await createDatabase();
        await using (var context = Context())
        {
            await context.Database.MigrateAsync();
        }
        var database = Substitute.For<IDatabase>();
        database.Namespace(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(_ => Task.FromResult(new DbContextScope<NamespaceDbContext>(Context(), () => { })));
        Storage = new("closed-streams-store", "closed-streams-namespace", EventSequenceId.Log, database);
    }

    public NamespaceDbContext Context()
    {
        var builder = provider == "postgresql"
            ? new DbContextOptionsBuilder<NamespaceDbContext>(SpecDbContextOptions.Kernel<NamespaceDbContext>(_connectionString))
            : new DbContextOptionsBuilder<NamespaceDbContext>().AddConceptAsSupport().WithoutServiceProviderCaching();
        switch (provider)
        {
            case "postgresql":
                builder.UseNpgsql(_connectionString);
                break;
            case "sql_server":
                builder.UseSqlServer(_connectionString);
                break;
            default:
                builder.UseSqlite(_connectionString);
                break;
        }

        return new(builder.Options);
    }

    public async Task<List<T>> Query<T>(FormattableString sql)
    {
        await using var context = Context();
        return await context.Database.SqlQuery<T>(sql).ToListAsync();
    }

    public async Task Execute(string sql)
    {
        await using var context = Context();
        await context.Database.ExecuteSqlRawAsync(sql);
    }

    /// <summary>
    /// Returns the closed streams table to the layout that existed before keys were compared exactly, then upgrades it again.
    /// </summary>
    /// <param name="legacyRows">SQL statements that fill the table while it is in the earlier layout.</param>
    /// <returns>Awaitable task.</returns>
    public async Task UpgradeFromEarlierLayout(params string[] legacyRows)
    {
        await Apply(false);
        foreach (var row in legacyRows) await Execute(row);
        await Apply(true);
    }

    public async Task Apply(bool up)
    {
        await using var context = Context();
        var migration = new v20_0_2 { ActiveProvider = context.Database.ProviderName };
        var operations = up ? migration.UpOperations : migration.DownOperations;
        foreach (var command in context.GetService<IMigrationsSqlGenerator>().Generate(operations))
        {
            await context.Database.ExecuteSqlRawAsync(command.CommandText);
        }
    }

    public async ValueTask DisposeAsync() => await dropDatabase(_connectionString);
}
