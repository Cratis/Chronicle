// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ensuring_indexes;

[Collection(PostgreSqlCollection.Name)]
public class and_nested_integers_exceed_int64_on_postgresql(PostgreSqlFixture fixture) : Specification
{
    const string Table = "unsigned_indexes";
    readonly Key _key = new("exponent", ArrayIndexers.NoIndexers);
    readonly CapturingIndexLogger _logger = new();
    readonly ExpandoObject _state = new();
    PostgreSqlSinkHarness _harness;
    DbContext _context;
    Sink _sink;
    string _inserted;
    string _updated;

    async Task Establish()
    {
        _harness = new() { Fixture = fixture, Logger = _logger };
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"},"details":{"type":"object","properties":{"total":{"type":"integer"},"rate":{"type":"number"}}}}}""");
        var definition = new ReadModelDefinition("exponent", Table, "Exponent", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "exponent", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, [new("details.total"), new("details.rate")]);
        _sink = (Sink)_harness.CreateSink(definition);
        await _sink.GetInstances();
        await _sink.EnsureIndexes();
        await _sink.IndexBuildCompletion.WaitAsync(TimeSpan.FromSeconds(30));
        _logger.Errors.ShouldBeEmpty();
        _logger.Warnings.ShouldBeEmpty();
        _context = new(SpecDbContextOptions.Kernel<DbContext>(_harness.ConnectionString));
        var name = ReadModelIndexPlan.Resolve(definition.Indexes.First(), schema, ProjectedColumns.ForSchema(schema), DatabaseType.PostgreSql)!.Name(DatabaseType.PostgreSql, Table);
        var rateName = ReadModelIndexPlan.Resolve(definition.Indexes.Last(), schema, ProjectedColumns.ForSchema(schema), DatabaseType.PostgreSql)!.Name(DatabaseType.PostgreSql, Table);
        (await _context.Database.SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM pg_indexes WHERE schemaname=current_schema() AND tablename={Table} AND (indexname={name} OR indexname={rateName})").SingleAsync()).ShouldEqual(2);
    }

    async Task Because()
    {
        await Write(ulong.MaxValue, 1E-7, 1UL);
        _inserted = await StoredJson();
        await Write(ulong.MaxValue - 1, 1E21, 2UL);
        _updated = await StoredJson();
    }

    [Fact]
    void should_insert_and_update_large_integers_and_exponent_numbers_with_active_nested_indexes()
    {
        using var inserted = JsonDocument.Parse(_inserted);
        using var updated = JsonDocument.Parse(_updated);
        inserted.RootElement.GetProperty("total").GetUInt64().ShouldEqual(ulong.MaxValue);
        updated.RootElement.GetProperty("total").GetUInt64().ShouldEqual(ulong.MaxValue - 1);
        inserted.RootElement.GetProperty("rate").GetDouble().ShouldEqual(1E-7);
        updated.RootElement.GetProperty("rate").GetDouble().ShouldEqual(1E21);
    }

    async Task Destroy()
    {
        if (_context is not null) await _context.DisposeAsync();
        _harness?.Dispose();
    }

    async Task Write(ulong total, double rate, EventSequenceNumber sequenceNumber)
    {
        var initial = _state;
        var details = new ExpandoObject();
        ((IDictionary<string, object?>)details)["total"] = total;
        ((IDictionary<string, object?>)details)["rate"] = rate;
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(initial);
        ((IDictionary<string, object?>)initial).TryGetValue("details", out var previous);
        changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(initial, [new PropertyDifference("details", previous, details)])]);
        (await _sink.ApplyChanges(_key, changeset, sequenceNumber)).ShouldBeEmpty();
        ((IDictionary<string, object?>)initial)["details"] = details;
    }

    Task<string> StoredJson() => _context.Database.SqlQuery<string>($"SELECT details::text AS \"Value\" FROM unsigned_indexes WHERE id={_key.Value}").SingleAsync();
}
