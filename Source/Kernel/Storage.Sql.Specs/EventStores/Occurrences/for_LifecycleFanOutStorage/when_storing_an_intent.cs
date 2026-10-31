// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Occurrences;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences.for_LifecycleFanOutStorage;

public class when_storing_an_intent : LifecycleFanOutContract, IDisposable
{
    SqliteConnection? _connection;
    string? _provider;
    string _connectionString;

    public void Dispose()
    {
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }

    async Task Establish()
    {
        _provider = Environment.GetEnvironmentVariable("CHRONICLE_SQL_SPECS_PROVIDER");
        _connectionString = Environment.GetEnvironmentVariable("CHRONICLE_SQL_SPECS_CONNECTION_STRING") ?? $"DataSource=lifecycle_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        if (_provider is null)
        {
            _connection = new SqliteConnection(_connectionString);
            await _connection.OpenAsync();
        }
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    protected override ILifecycleFanOutStorage OpenStorage() => new LifecycleFanOutStorage(() => Task.FromResult(new DbContextScope<EventStoreDbContext>(CreateContext(), static () => { })));

    EventStoreDbContext CreateContext()
    {
        var builder = new DbContextOptionsBuilder<EventStoreDbContext>();
        switch (_provider)
        {
            case "PostgreSQL": builder.UseNpgsql(_connectionString); break;
            case "SQLServer": builder.UseSqlServer(_connectionString); break;
            default: builder.UseSqlite(_connectionString); break;
        }
        builder.AddConceptAsSupport().WithoutServiceProviderCaching();
        return new(builder.Options);
    }
}
