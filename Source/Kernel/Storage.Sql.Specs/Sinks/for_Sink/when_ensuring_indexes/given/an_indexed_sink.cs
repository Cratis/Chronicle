// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ensuring_indexes.given;

public abstract class an_indexed_sink : Specification
{
    protected ISqlSinkHarness _harness;
    protected ISink _sink;
    protected int _initialCount;
    protected int _repeatedCount;
    protected int _shadowCount;
    protected int _promotedCount;
    protected int _secondShadowCount;
    protected int _secondPromotedCount;
    protected string[] _indexedColumns;
    protected string[] _names;
    const string Table = "indexed-models";

    protected virtual string CompoundLead => "name";

    protected abstract ISqlSinkHarness CreateHarness();

    async Task Establish()
    {
        _harness = CreateHarness();
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"},"number]quote":{"type":"integer"},"name":{"type":"string"},"nested":{"type":"object","properties":{"value":{"type":"integer"}}},"lines":{"type":"array","items":{"type":"object","properties":{"value":{"type":"integer"}}}}}}""");
        _sink = _harness.CreateSink(new ReadModelDefinition(
            "indexes",
            Table,
            "Indexes",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            "index-projection",
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema },
            new[] { "number]quote", "name", "nested", "nested.value", "lines", "lines.value", "missing" }.Select(path => new IndexDefinition(new PropertyPath(path))).Concat(
            [
                new IndexDefinition(CompoundLead) { Properties = [new(CompoundLead), new("nested.value", true)] },
                new IndexDefinition("number]quote") { Properties = [new("number]quote", true)] }
            ]).ToArray()));
    }

    protected async Task EnsureAndReplay()
    {
        await _sink.EnsureIndexes();
        _initialCount = (await Indexes(Table)).Length;
        await _sink.EnsureIndexes();
        var initial = await Indexes(Table);
        _repeatedCount = initial.Length;
        _indexedColumns = initial.Select(index => index.Column).Order().ToArray();
        _names = initial.Select(index => index.Name).ToArray();
        var replay = new ReplayContext(new ReadModelType("indexes", ReadModelGeneration.First), Table, $"{Table}-revert", DateTimeOffset.UtcNow);
        await _sink.BeginReplay(replay);
        _shadowCount = (await Indexes($"replay-{Table}")).Length;
        await _sink.EndReplay(replay);
        _promotedCount = (await Indexes(Table)).Length;
        await _sink.EnsureIndexes();
        (await Indexes(Table)).Length.ShouldEqual(_initialCount);
        replay = replay with { RevertContainerName = $"{Table}-second-revert" };
        await _sink.BeginReplay(replay);
        _secondShadowCount = (await Indexes($"replay-{Table}")).Length;
        await _sink.EndReplay(replay);
        _secondPromotedCount = (await Indexes(Table)).Length;
    }

    async Task<IndexRow[]> Indexes(string table)
    {
        var options = new DbContextOptionsBuilder<DbContext>().UseDatabaseFromConnectionString(_harness.ConnectionString).Options;
        await using var context = new DbContext(options);
        var query = context.Database.GetDatabaseType() switch
        {
            DatabaseType.PostgreSql => context.Database.SqlQuery<IndexRow>($"SELECT i.relname AS \"Name\", COALESCE(a.attname, '<expression>') AS \"Column\" FROM pg_index x JOIN pg_class t ON t.oid = x.indrelid JOIN pg_namespace n ON n.oid = t.relnamespace JOIN pg_class i ON i.oid = x.indexrelid LEFT JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = x.indkey[0] WHERE t.relname = {table} AND n.nspname = current_schema() AND NOT x.indisprimary"),
            DatabaseType.SqlServer => context.Database.SqlQuery<IndexRow>($"SELECT i.name AS [Name], CASE WHEN c.is_computed=1 THEN '<expression>' ELSE c.name END AS [Column] FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID({table}) AND ic.key_ordinal = 1 AND i.is_primary_key = 0"),
            _ => context.Database.SqlQuery<IndexRow>($"SELECT i.name AS Name, COALESCE(c.name, '<expression>') AS \"Column\" FROM pragma_index_list({table}) i JOIN pragma_index_info(i.name) c ON c.seqno=0 WHERE i.origin = 'c'")
        };
        return (await query.ToArrayAsync()).Where(index => !index.Name.StartsWith("chronicle_imx_", StringComparison.Ordinal)).ToArray();
    }

    void Destroy() => _harness.Dispose();

    protected sealed class IndexRow
    {
        public string Name { get; set; } = string.Empty;
        public string Column { get; set; } = string.Empty;
    }
}
