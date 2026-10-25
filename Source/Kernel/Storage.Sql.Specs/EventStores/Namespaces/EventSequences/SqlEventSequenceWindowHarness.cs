// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>
/// Runs the shared window contract against SQL storage, using the configured provider or SQLite.
/// </summary>
public class SqlEventSequenceWindowHarness : IEventSequenceWindowHarness
{
    readonly string _tableName = $"window_{Guid.NewGuid():N}";
    readonly string? _provider = Environment.GetEnvironmentVariable("CHRONICLE_SQL_SPECS_PROVIDER");
    readonly string _connectionString = Environment.GetEnvironmentVariable("CHRONICLE_SQL_SPECS_CONNECTION_STRING") ?? $"DataSource=window_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    readonly IEventSequenceMigrator _migrator = new EventSequenceMigrator(
        new TableMigrator<EventSequenceDbContext>(Substitute.For<ILogger<TableMigrator<EventSequenceDbContext>>>()),
        Substitute.For<ILogger<EventSequenceMigrator>>());
    SqliteConnection? _connection;

    /// <inheritdoc/>
    public async Task<IEventSequenceStorage> CreateStorage()
    {
        if (_provider is null)
        {
            _connection = new SqliteConnection(_connectionString);
            await _connection.OpenAsync();
        }

        await using (var context = CreateContext())
        {
            await context.EnsureTableExists();
        }

        var database = Substitute.For<IDatabase>();
        database.EventSequenceTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>())
            .Returns(_ => Task.FromResult(new DbContextScope<EventSequenceDbContext>(CreateContext(), () => { })));
        var identities = Substitute.For<IIdentityStorage>();
        identities.GetFor(Arg.Any<IEnumerable<IdentityId>>()).Returns(Identity.System);
        return new EventSequenceStorage(
            "window-store",
            "window-namespace",
            _tableName,
            database,
            identities,
            Substitute.For<ILogger<EventSequenceStorage>>(),
            Substitute.For<IEventTypesStorage>(),
            new Json.ExpandoObjectConverter(new Schemas.TypeFormats()));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }

    EventSequenceDbContext CreateContext()
    {
        var builder = new DbContextOptionsBuilder<EventSequenceDbContext>();
        switch (_provider)
        {
            case "PostgreSQL": builder.UseNpgsql(_connectionString); break;
            case "SQLServer": builder.UseSqlServer(_connectionString); break;
            default: builder.UseSqlite(_connectionString); break;
        }
        builder.AddConceptAsSupport().WithoutServiceProviderCaching();
        return new EventSequenceDbContext(builder.Options, _tableName, _migrator);
    }
}
