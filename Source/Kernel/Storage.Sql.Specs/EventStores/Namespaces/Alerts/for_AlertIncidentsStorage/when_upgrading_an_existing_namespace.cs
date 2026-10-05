// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Alerts.for_AlertIncidentsStorage;

public class when_upgrading_an_existing_namespace : Specification
{
    an_existing_namespace _harness;
    bool _observerRetained;
    bool _incidentTableExists;

    void Establish() => _harness = new();

    async Task Because()
    {
        var storage = await _harness.Create();
        _observerRetained = await _harness.ObserverExists();
        _incidentTableExists = !(await storage.EnumerateOpen(null, 100)).Items.Any();
    }

    async Task Destroy() => await _harness.DisposeAsync();

    [Fact] void should_retain_existing_observer_rows() => _observerRetained.ShouldBeTrue();
    [Fact] void should_add_queryable_incident_storage() => _incidentTableExists.ShouldBeTrue();

    class an_existing_namespace : SqlAlertIncidentsHarness
    {
        public async Task<bool> ObserverExists()
        {
            await using var context = Context();

            return await context.Observers.AnyAsync(row => row.Id == "existing-observer" && row.NextEventSequenceNumber == 5UL);
        }

        protected override async Task<string> ConnectionString()
        {
            _connectionString = await base.ConnectionString();
            await using var context = Context();
            var assembly = context.GetService<IMigrationsAssembly>();
            var history = context.GetService<IHistoryRepository>();
            var sqlGenerator = context.GetService<IMigrationsSqlGenerator>();
            await context.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript());

            // Build the complete pre-incident namespace schema and its real migration history.
            foreach (var (id, type) in assembly.Migrations.Where(entry => !entry.Key.Contains("AlertIncidents", StringComparison.Ordinal)))
            {
                var migration = assembly.CreateMigration(type, context.Database.ProviderName);
                foreach (var command in sqlGenerator.Generate(migration.UpOperations))
                {
                    await context.Database.ExecuteSqlRawAsync(command.CommandText);
                }
                await context.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(id, "10.0.0")));
            }
            context.Observers.Add(new Observers.ObserverState
            {
                Id = "existing-observer",
                NextEventSequenceNumber = 5,
                LastHandledEventSequenceNumber = 4,
                TailEventSequenceNumber = 5
            });
            await context.SaveChangesAsync();

            return _connectionString;
        }
    }
}
