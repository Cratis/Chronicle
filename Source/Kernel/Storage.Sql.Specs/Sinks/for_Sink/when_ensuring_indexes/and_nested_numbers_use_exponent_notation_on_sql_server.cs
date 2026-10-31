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

[Collection(SqlServerCollection.Name)]
public class and_nested_numbers_use_exponent_notation_on_sql_server(SqlServerFixture fixture) : Specification
{
    const string Table = "exponent_indexes";
    readonly Key _key = new("exponent", ArrayIndexers.NoIndexers);
    readonly CapturingIndexLogger _logger = new();
    SqlServerSinkHarness _harness;
    DbContext _context;
    Sink _sink;
    string _inserted;
    string _updated;
    string _indexName;

    async Task Establish()
    {
        _harness = new() { Fixture = fixture, Logger = _logger };
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"},"details":{"type":"object","properties":{"total":{"type":"number"}}}}}""");
        var definition = new ReadModelDefinition("exponent", Table, "Exponent", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "exponent", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, [new("details.total")]);
        _sink = (Sink)_harness.CreateSink(definition);
        await _sink.GetInstances();
        await _sink.EnsureIndexes();
        await _sink.IndexBuildCompletion.WaitAsync(TimeSpan.FromSeconds(30));
        _logger.Errors.ShouldBeEmpty();
        _logger.Warnings.ShouldBeEmpty();
        _context = new(new DbContextOptionsBuilder().UseDatabaseFromConnectionString(_harness.ConnectionString).Options);
        _indexName = ReadModelIndexPlan.Resolve(definition.Indexes.Single(), schema, ProjectedColumns.ForSchema(schema), DatabaseType.SqlServer)!.Name(DatabaseType.SqlServer, Table);
        (await _context.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM sys.indexes WHERE object_id=OBJECT_ID({Table}) AND name={_indexName} AND is_disabled=0").SingleAsync()).ShouldEqual(1);
    }

    async Task Because()
    {
        await Write(1E-7, 1UL);
        _inserted = await StoredJson();
        await Write(1E21, 2UL);
        _updated = await StoredJson();
    }

    [Fact]
    void should_insert_and_update_exponent_form_numbers_with_an_active_nested_index()
    {
        _inserted.ShouldEqual(JsonSerializer.Serialize(new { total = 1E-7 }));
        _updated.ShouldEqual(JsonSerializer.Serialize(new { total = 1E21 }));
    }

    [Fact]
    async Task should_compare_nested_values_as_bounded_text()
    {
        var value = JsonSerializer.Serialize(1E21);
        (await _context.Database.SqlQuery<string>($"SELECT [id] AS [Value] FROM [exponent_indexes] WHERE CAST(JSON_VALUE([details], '$.\"total\"') AS nvarchar(450))={value}").SingleAsync()).ShouldEqual(_key.Value);
    }

    [Fact]
    async Task should_order_nested_numbers_lexically()
    {
        (await _context.Database.SqlQuery<string>($"SELECT [id] AS [Value] FROM [exponent_indexes] WHERE CAST(JSON_VALUE([details], '$.\"total\"') AS nvarchar(450)) < N'2'").SingleAsync()).ShouldEqual(_key.Value);
    }

    async Task Destroy()
    {
        if (_context is not null) await _context.DisposeAsync();
        _harness?.Dispose();
    }

    async Task Write(double total, EventSequenceNumber sequenceNumber)
    {
        var initial = await _sink.FindOrDefault(_key) ?? new ExpandoObject();
        var details = new ExpandoObject();
        ((IDictionary<string, object?>)details)["total"] = total;
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(initial);
        ((IDictionary<string, object?>)initial).TryGetValue("details", out var previous);
        changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(initial, [new PropertyDifference("details", previous, details)])]);
        (await _sink.ApplyChanges(_key, changeset, sequenceNumber)).ShouldBeEmpty();
    }

    Task<string> StoredJson() => _context.Database.SqlQuery<string>($"SELECT [details] AS [Value] FROM [exponent_indexes] WHERE [id]={_key.Value}").SingleAsync();
}
