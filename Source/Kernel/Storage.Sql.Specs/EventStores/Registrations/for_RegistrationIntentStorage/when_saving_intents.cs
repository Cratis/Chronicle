// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Registrations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Registrations.for_RegistrationIntentStorage;

public class when_saving_intents : RegistrationIntentStorageContract, IDisposable
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
        _connectionString = Environment.GetEnvironmentVariable("CHRONICLE_SQL_SPECS_CONNECTION_STRING") ?? $"DataSource=registrations_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        if (_provider is null)
        {
            _connection = new SqliteConnection(_connectionString);
            await _connection.OpenAsync();
        }

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    protected override IRegistrationIntentStorage OpenStorage()
    {
        var database = Substitute.For<IDatabase>();
        database.EventStore(Arg.Any<EventStoreName>()).Returns(_ => Task.FromResult(new DbContextScope<EventStoreDbContext>(CreateContext(), static () => { })));
        return new RegistrationIntentStorage("registrations-store", database);
    }

    EventStoreDbContext CreateContext()
    {
        if (_provider == "PostgreSQL")
        {
            return new(SpecDbContextOptions.Kernel<EventStoreDbContext>(_connectionString));
        }

        var builder = new DbContextOptionsBuilder<EventStoreDbContext>();
        switch (_provider)
        {
            case "SQLServer": builder.UseSqlServer(_connectionString); break;
            default: builder.UseSqlite(_connectionString); break;
        }

        builder.AddConceptAsSupport().WithoutServiceProviderCaching();
        return new(builder.Options);
    }
}
