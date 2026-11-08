// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sql.EventStores.ReadModels.Migrations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using KernelDefinition = Cratis.Chronicle.Concepts.ReadModels.ReadModelDefinition;

namespace Cratis.Chronicle.Storage.Sql.EventStores.ReadModels.for_ReadModelDefinition;

public class when_upgrading_sink_names : Specification
{
    SqliteConnection _connection;
    EventStoreDbContext _context;
    KernelDefinition _definition;
    KernelDefinition _roundTripped;
    KernelDefinition _legacy;

    async Task Establish()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        _context = new EventStoreDbContext(new DbContextOptionsBuilder<EventStoreDbContext>().UseSqlite(_connection).AddConceptAsSupport().Options);
        _definition = new KernelDefinition(
            "reporting-model",
            "reporting",
            "Reporting",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            "projection",
            new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.SQL) { Name = "reporting" },
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"}}}""") },
            []);
        await Apply(new v15_0_0());
        await Apply(new v19_93_0());
        await _context.Database.ExecuteSqlAsync($"INSERT INTO ReadModelDefinitions (Id, Name, DisplayName, Owner, Source, ObserverType, ObserverIdentifier, SinkType, SinkConfigurationId, Schemas, IndexesJson) VALUES ('legacy', 'legacy', 'Legacy', 0, 0, 0, 'projection', 'MongoDB', '00000000-0000-0000-0000-000000000000', '{{}}', '[]')");
    }

    async Task Because()
    {
        await Apply(new v19_143_0());
        _context.ReadModels.Add(_definition.ToSql());
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        _roundTripped = (await _context.ReadModels.SingleAsync(model => model.Id == _definition.Identifier)).ToKernel();
        _legacy = (await _context.ReadModels.SingleAsync(model => model.Id == "legacy")).ToKernel();
    }

    async Task Apply(Migration migration)
    {
        migration.ActiveProvider = _context.Database.ProviderName;
        var generator = _context.GetService<IMigrationsSqlGenerator>();
        foreach (var command in generator.Generate(migration.UpOperations, _context.Model))
        {
#pragma warning disable EF1002 // SQL comes from EF's migrations generator.
            await _context.Database.ExecuteSqlRawAsync(command.CommandText);
#pragma warning restore EF1002
        }
    }

    async Task Destroy()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact] void should_round_trip_the_sink_name() => _roundTripped.Sink.Name.ShouldEqual(new SinkName("reporting"));
    [Fact] void should_round_trip_the_sink_type() => _roundTripped.Sink.Type.ShouldEqual(WellKnownSinkTypes.SQL);
    [Fact] void should_give_existing_rows_the_default_sink() => _legacy.Sink.Name.IsDefault.ShouldBeTrue();
}
