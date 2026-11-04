// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.EventSequences.Migrations;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage.given;

public class a_populated_migration_chain : Specification, IDisposable
{
    protected EventTypeDefinition _chain;
    protected EventTypeDefinition _stale;
    protected EventTypeMigrationsVersion _version;
    protected EventTypesStorage _storage;
    protected IDatabase _database;
    protected DbContextOptions<EventStoreDbContext> _options;
    protected EventType _before;
    protected EventTypeDefinition _warm;
    protected EventTypeSchema _warmSchema;

    SqliteConnection? _connection;

    async Task Establish()
    {
        await CreateDatabase();
        await using (var context = CreateContext())
        {
            await context.Database.EnsureCreatedAsync();
        }

        _database = Substitute.For<IDatabase>();
        _database.EventStore(Arg.Any<EventStoreName>()).Returns(_ => Task.FromResult(new DbContextScope<EventStoreDbContext>(CreateContext(), () => { })));
        _storage = new(EventStoreName.NotSet, _database);
        _chain = new(
            "additive-chain",
            EventTypeOwner.Client,
            false,
            [new(1, Schema("one")), new(2, Schema("two")), new(3, Schema("three"))],
            [
                new(1, 2, [], new JsonObject { ["two"] = "one" }, new JsonObject { ["one"] = "two" }),
                new(2, 3, [], new JsonObject { ["three"] = "two" }, new JsonObject { ["two"] = "three" })
            ]);
        _stale = _chain with { Generations = _chain.Generations.Take(2).ToArray(), Migrations = _chain.Migrations.Take(1).ToArray() };
        await _storage.Register(_chain);
        await using (var context = CreateContext())
        {
            var row = await context.EventTypes.SingleAsync(_ => _.Id == _chain.Id);
            row.Visibility = EventTypeVisibility.Public;
            await context.SaveChangesAsync();
        }
        _storage.Invalidate(_chain.Id);
        _version = EventTypeMigrationsVersion.For(_chain.Migrations);
        await _storage.RecordMigrationsVersion(_chain.Id, _version, _chain.Migrations);
        _before = await ReadRow();
        _warm = await _storage.GetDefinition(_chain.Id);
        _warmSchema = await _storage.GetFor(_chain.Id, 3);
    }

    protected virtual async Task CreateDatabase()
    {
        _connection = new("DataSource=:memory:");
        await _connection.OpenAsync();
        _options = new DbContextOptionsBuilder<EventStoreDbContext>().UseSqlite(_connection).AddConceptAsSupport().WithoutServiceProviderCaching().Options;
    }

    protected virtual Task DropDatabase()
    {
        _connection?.Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }

    protected EventStoreDbContext CreateContext() => new(_options);

    protected async Task<EventType> ReadRow()
    {
        await using var context = CreateContext();
        return await context.EventTypes.SingleAsync(_ => _.Id == _chain.Id);
    }

    protected Task<EventTypeDefinition> ReadCold() => new EventTypesStorage(EventStoreName.NotSet, _database).GetDefinition(_chain.Id);

    protected Task<IEnumerable<EventTypeId>> RegisterBatch(EventTypeDefinition definition, EventTypeVisibility visibility = EventTypeVisibility.Unspecified) =>
        ((IEventTypesStorage)_storage).Register([new(definition, EventTypeSource.Code, visibility)]);

    protected void ShouldRetainSchemas(EventTypeDefinition actual) => actual.Generations.OrderBy(_ => _.Generation.Value).Select(_ => _.Schema.ToJson()).SequenceEqual(_chain.Generations.Select(_ => _.Schema.ToJson())).ShouldBeTrue();

    protected void ShouldRetainBridges(EventTypeDefinition actual) => EventTypeMigrationsVersion.For(actual.Migrations).ShouldEqual(_version);

    protected static async Task ShouldMigrateAcrossChain(EventTypeDefinition definition, (uint Generation, string Property) from, (uint Generation, string Property) to)
    {
        var converter = new ExpandoObjectConverter(new TypeFormats());
        var engine = new EventTypeMigrations(Substitute.For<IStorage>(), converter);
        var content = new JsonObject { [from.Property] = "payload" };
        var schema = definition.Generations.Single(_ => _.Generation.Value == from.Generation).Schema;
        var result = await engine.MigrateToAllGenerations(definition, new(definition.Id, from.Generation), content, converter.ToExpandoObject(content, schema));
        result.Count.ShouldEqual(3);
        ((IDictionary<string, object?>)result[(EventTypeGeneration)to.Generation])[to.Property].ShouldEqual("payload");
    }

    protected static EventTypeMigrationDefinition Bridge(uint from, uint to, string up, string down) => new(from, to, [], new JsonObject { ["value"] = up }, new JsonObject { ["value"] = down });

    static JsonSchema Schema(string marker)
    {
        var schema = JsonSchema.FromJson($$$$"""{"type":"object","description":"{{{{marker}}}}","properties":{"{{{{marker}}}}":{"type":"string"}}}""");
        schema.EnsureComplianceMetadata();
        return schema;
    }

    async Task Destroy() => await DropDatabase();
}
