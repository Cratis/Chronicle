// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.for_EncryptionKeyStorage.given;

public sealed class KeyStoreHarness(string provider, Func<Task<string>> createDatabase, Func<string, Task> dropDatabase) : IAsyncDisposable
{
    string _connectionString;
    readonly CommandCounter _commands = new();
    bool _counting;
    public EncryptionKeyStorage Storage { get; private set; }

    public int Commands => _commands.Count;

    public DbCommandInterceptor Interceptor { get; set; }

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
        Storage = new(database);
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

        if (_counting) builder.AddInterceptors(_commands);
        if (Interceptor is not null) builder.AddInterceptors(Interceptor);

        return new(builder.Options);
    }

    public void CountCommands() => _counting = true;

    public async Task<bool> Exists(string table) => (await Query<string>($"SELECT name AS [Value] FROM sys.tables WHERE name = {table}")).Count > 0;

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
    /// Returns the encryption key tables to the layout that existed before identifiers were compared exactly, then upgrades them again.
    /// </summary>
    /// <param name="legacyRows">SQL statements that fill the tables while they are in the earlier layout.</param>
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
        var migration = new v20_0_1 { ActiveProvider = context.Database.ProviderName };
        var operations = up ? migration.UpOperations : migration.DownOperations;
        foreach (var command in context.GetService<IMigrationsSqlGenerator>().Generate(operations))
        {
            await context.Database.ExecuteSqlRawAsync(command.CommandText);
        }
    }

    public async Task<string> CollationOf(string table)
    {
        await using var context = Context();
        return (await context.Database.SqlQuery<string>($"SELECT collation_name AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID({table}) AND name = 'Identifier'").ToListAsync()).Single();
    }

    public async ValueTask DisposeAsync() => await dropDatabase(_connectionString);
}
