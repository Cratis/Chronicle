// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Occurrences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences.for_OccurrencesStorage.given;

public class an_occurrence_storage : Specification, IDisposable
{
    protected readonly TriggerId _trigger = new(Guid.NewGuid().ToString("N"));
    protected readonly DateTimeOffset _now = DateTimeOffset.UnixEpoch.AddDays(1);
    protected ScheduleToken _token;
    protected IOccurrencesStorage _storage;
    protected IScheduleStorage _schedules;
    protected Occurrence _occurrence;
    string? _provider;
    string _connectionString;
    SqliteConnection? _connection;

    public void Dispose()
    {
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gets whether contexts use a retrying execution strategy, as PostgreSQL connections do.
    /// </summary>
    protected virtual bool UsesARetryingExecutionStrategy => false;

    protected IOccurrencesStorage Recreate() => new OccurrencesStorage<NamespaceDbContext>(Open);
    protected IScheduleStorage RecreateSchedules() => new OccurrencesStorage<NamespaceDbContext>(Open);

    async Task Establish()
    {
        _provider = Environment.GetEnvironmentVariable("CHRONICLE_SQL_SPECS_PROVIDER");
        _connectionString = Environment.GetEnvironmentVariable("CHRONICLE_SQL_SPECS_CONNECTION_STRING") ?? $"DataSource=occurrences_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        if (_provider is null)
        {
            _connection = new SqliteConnection(_connectionString);
            await _connection.OpenAsync();
        }

        await using (var context = CreateContext())
        {
            await context.Database.MigrateAsync();
        }

        var concrete = new OccurrencesStorage<NamespaceDbContext>(Open);
        _storage = concrete;
        _schedules = concrete;
        _token = new ScheduleToken(_trigger.Value);
        _occurrence = new(Guid.NewGuid().ToString("N"), _trigger, OccurrenceKind.Application, "first", "{\"value\":1}", 1, _now, null, null, OccurrenceState.Pending, 0, 0, null, null, null, null);
    }

    Task<DbContextScope<NamespaceDbContext>> Open() => Task.FromResult(new DbContextScope<NamespaceDbContext>(CreateContext(), static () => { }));

    NamespaceDbContext CreateContext()
    {
        var builder = new DbContextOptionsBuilder<NamespaceDbContext>();
        switch (_provider)
        {
            case "PostgreSQL": builder.UseNpgsql(_connectionString); break;
            case "SQLServer": builder.UseSqlServer(_connectionString); break;
            default:
                builder.UseSqlite(_connectionString, sqlite =>
                {
                    if (UsesARetryingExecutionStrategy) sqlite.ExecutionStrategy(dependencies => new RetryingExecutionStrategy(dependencies));
                });
                break;
        }

        builder.AddConceptAsSupport().WithoutServiceProviderCaching();
        return new NamespaceDbContext(builder.Options);
    }
}
