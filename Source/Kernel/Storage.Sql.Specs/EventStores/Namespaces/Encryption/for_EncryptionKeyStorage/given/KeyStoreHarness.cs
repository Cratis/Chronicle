// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.for_EncryptionKeyStorage.given;

public sealed class KeyStoreHarness(string provider, Func<Task<string>> createDatabase, Func<string, Task> dropDatabase) : IAsyncDisposable
{
    string _connectionString;
    public EncryptionKeyStorage Storage { get; private set; }

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

        return new(builder.Options);
    }

    public async Task<string> CollationOf(string table)
    {
        await using var context = Context();
        return (await context.Database.SqlQuery<string>($"SELECT collation_name AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID({table}) AND name = 'Identifier'").ToListAsync()).Single();
    }

    public async ValueTask DisposeAsync() => await dropDatabase(_connectionString);
}
