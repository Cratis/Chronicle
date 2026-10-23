// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sql.EventStores.ReadModels.Migrations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using KernelDefinition = Cratis.Chronicle.Concepts.ReadModels.ReadModelDefinition;

namespace Cratis.Chronicle.Storage.Sql.EventStores.ReadModels.for_ReadModelDefinition;

public class when_upgrading_index_metadata : Specification
{
    SqliteConnection _connection;
    EventStoreDbContext _context;
    KernelDefinition _definition;
    KernelDefinition _roundTripped;
    KernelDefinition _legacy;
    KernelDefinition _legacyNull;

    async Task Establish()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        _context = new EventStoreDbContext(new DbContextOptionsBuilder<EventStoreDbContext>().UseSqlite(_connection).AddConceptAsSupport().Options);
        _definition = new KernelDefinition(
            "indexed",
            "indexes",
            "Indexed",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            "projection",
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"},"name":{"type":"string"}}}""") },
            [new IndexDefinition(new PropertyPath("name")), new IndexDefinition(new PropertyPath("lines.productId"))]);
        await Apply(new v15_0_0());
        await _context.Database.ExecuteSqlAsync($"INSERT INTO ReadModelDefinitions (Id, Name, DisplayName, Owner, Source, ObserverType, ObserverIdentifier, SinkType, SinkConfigurationId, Schemas) VALUES ('legacy', 'legacy', 'Legacy', 0, 0, 0, 'projection', '', '00000000-0000-0000-0000-000000000000', '{{}}')");
    }

    async Task Because()
    {
        await Apply(new v19_93_0());
        _context.ReadModels.Add(_definition.ToSql());
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        _roundTripped = (await _context.ReadModels.SingleAsync(model => model.Id == _definition.Identifier)).ToKernel();
        _legacy = (await _context.ReadModels.SingleAsync(model => model.Id == "legacy")).ToKernel();
        var old = _definition.ToSql();
        old.IndexesJson = null!;
        _legacyNull = old.ToKernel();
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

    [Fact] void should_round_trip_every_index_path() => _roundTripped.Indexes.Select(index => index.PropertyPath.Path).ShouldEqual(["name", "lines.productId"]);
    [Fact] void should_preserve_schemas() => _roundTripped.GetSchemaForLatestGeneration().ToJson().ShouldEqual(_definition.GetSchemaForLatestGeneration().ToJson());
    [Fact] void should_keep_existing_rows_without_indexes() => _legacy.Indexes.ShouldBeEmpty();
    [Fact] void should_treat_legacy_null_as_no_indexes() => _legacyNull.Indexes.ShouldBeEmpty();
}
