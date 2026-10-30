// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations.Schema;
using System.Dynamic;
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

#pragma warning disable EF1002 // Constant identifiers and internal index names only; values are parameterized.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_storing_decimal_values.given;

public abstract class a_sqlite_ordered_decimal_index_sink : Specification
{
    protected const string Table = "ordered_decimals";
    protected SqlSinkHarness _harness;
    protected Sink _sink;
    protected ReadModelDefinition _definition;
    internal ReadModelIndexPlan _indexPlan;

    protected abstract IndexDefinition Index { get; }

    void Establish()
    {
        _harness = new SqlSinkHarness();
        var schema = JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"string"},"amount":{"type":"number","format":"decimal"},"name":{"type":"string"}}}""");
        _definition = new ReadModelDefinition("ordered-decimals", Table, "Ordered decimals", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "ordered-decimals", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, [Index]);
        _sink = (Sink)_harness.CreateSink(_definition);
        _indexPlan = ReadModelIndexPlan.Resolve(Index, schema, ProjectedColumns.ForSchema(schema, [Index]), DatabaseType.Sqlite);
    }

    protected async Task Write(string id, string name, decimal? amount)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("amount", null, amount), new PropertyDifference("name", null, name)])]);
        (await _sink.ApplyChanges(new Key(id, ArrayIndexers.NoIndexers), changeset, 1UL)).ShouldBeEmpty();
    }

    protected async Task WriteAmounts(string name)
    {
        await Write($"{name}-2", name, 2m);
        await Write($"{name}-10", name, 10m);
        await Write($"{name}-100", name, 100m);
        await Write($"{name}-minus", name, -10m);
    }

    protected DbContext OpenContext() => new(new DbContextOptionsBuilder().UseSqlite(_harness.ConnectionString).Options);

    protected static async Task<string[]> Indexes(DbContext context, string table) => await context.Database.SqlQuery<string>($"SELECT name AS Value FROM pragma_index_list({table})").ToArrayAsync();

    protected static async Task<string[]> Amounts(DbContext context, string where, string orderBy) =>
        await context.Database.SqlQueryRaw<string>($"SELECT amount AS Value FROM {Table} WHERE {where} ORDER BY {orderBy}").ToArrayAsync();

    protected void Destroy() => _harness.Dispose();

    protected sealed class QueryPlan
    {
        [Column("id")]
        public long Id { get; set; }
        [Column("parent")]
        public long Parent { get; set; }
        [Column("notused")]
        public long NotUsed { get; set; }
        [Column("detail")]
        public string Detail { get; set; } = string.Empty;
    }
}
