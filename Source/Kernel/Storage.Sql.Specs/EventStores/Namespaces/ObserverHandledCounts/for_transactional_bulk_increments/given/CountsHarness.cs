// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverHandledCounts.for_transactional_bulk_increments.given;

public sealed class CountsHarness(string provider, Func<Task<string>> createDatabase, Func<string, Task> dropDatabase) : IAsyncDisposable
{
    string _connectionString;
    public CountsCommands Commands { get; } = new();
    public CommitAcknowledgmentFailure Commit { get; } = new();
    public int Scopes { get; private set; }
    public ObserverHandledCountsStorage Storage { get; private set; }

    public async Task Initialize()
    {
        _connectionString = await createDatabase();
        await using (var context = Context(false))
        {
            await context.Database.MigrateAsync();
        }
        var database = Substitute.For<IDatabase>();
        database.Namespace(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>()).Returns(_ =>
        {
            Scopes++;
            return Task.FromResult(new DbContextScope<NamespaceDbContext>(Context(true), () => { }));
        });
        Storage = new(EventStoreName.System, EventStoreNamespaceName.Default, database);
    }

    public NamespaceDbContext Context(bool intercept = false)
    {
        var builder = provider == "postgresql"
            ? new DbContextOptionsBuilder<NamespaceDbContext>(SpecDbContextOptions.Kernel<NamespaceDbContext>(_connectionString))
            : new DbContextOptionsBuilder<NamespaceDbContext>().AddConceptAsSupport().WithoutServiceProviderCaching();
        switch (provider)
        {
            case "postgresql":
                builder.UseNpgsql(_connectionString, options => options.EnableRetryOnFailure(2, TimeSpan.Zero, null));
                break;
            case "sql_server":
                builder.UseSqlServer(_connectionString, options => options.EnableRetryOnFailure(2, TimeSpan.Zero, null));
                break;
            default:
                builder.UseSqlite(_connectionString);
                break;
        }
        if (intercept) builder.AddInterceptors(Commands, Commit);

        return new(builder.Options);
    }

    public void Reset()
    {
        Scopes = 0;
        Commands.Reset();
        Commit.Reset();
    }

    public async Task<ObserverHandledCountEntry[]> Rows()
    {
        await using var context = Context();
        return await context.ObserverHandledCounts.AsNoTracking().ToArrayAsync();
    }

    public async ValueTask DisposeAsync() => await dropDatabase(_connectionString);
}
