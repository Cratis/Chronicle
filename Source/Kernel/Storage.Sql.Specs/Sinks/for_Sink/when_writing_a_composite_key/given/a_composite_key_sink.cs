// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_writing_a_composite_key.given;

public abstract class a_composite_key_sink : Specification
{
    protected const string Table = "composite_shadows";
    protected ISqlSinkHarness _harness;
    protected ISink _sink;
    protected StoredRow[] _rows;
    protected int _shadowColumns;
    protected int _shadowIndexes;

    protected abstract ISqlSinkHarness CreateHarness();

    protected virtual JsonSchema CreateSchema() => JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"object","properties":{"first":{"type":"string"},"second":{"type":"string"}}},"name":{"type":"string"}}}""");

    void Establish()
    {
        _harness = CreateHarness();
        var schema = CreateSchema();
        _sink = _harness.CreateSink(new ReadModelDefinition("composite", Table, "Composite", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "composite", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []));
    }

    protected async Task WriteAndInspect()
    {
        await Write("a", "b", "first");
        await Write("a", "b", "second");
        await using var context = CreateContext();
        var helper = context.GetService<ISqlGenerationHelper>();
        var table = helper.DelimitIdentifier(Table);
#pragma warning disable EF1002 // All identifiers are delimited; data is not interpolated.
        _rows = await context.Database.SqlQueryRaw<StoredRow>($"SELECT {helper.DelimitIdentifier("id")} AS {helper.DelimitIdentifier("Id")}, {helper.DelimitIdentifier("name")} AS {helper.DelimitIdentifier("Name")}, {helper.DelimitIdentifier("__compositeKey")} AS {helper.DelimitIdentifier("Stamp")} FROM {table}").ToArrayAsync();
#pragma warning restore EF1002
        _shadowColumns = await CountShadowColumns(context);
        _shadowIndexes = await CountShadowIndexes(context);
    }

    protected Task Write(string first, string second, string name)
    {
        var parts = new ExpandoObject();
        ((IDictionary<string, object?>)parts)["second"] = second;
        ((IDictionary<string, object?>)parts)["first"] = first;
        return Write(parts, name);
    }

    protected Task Write(ExpandoObject parts, string name)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("name", null, name)])]);
        return _sink.ApplyChanges(new Key(parts, ArrayIndexers.NoIndexers), changeset, EventSequenceNumber.First);
    }

    protected DbContext CreateContext() => new(new DbContextOptionsBuilder<DbContext>().UseDatabaseFromConnectionString(_harness.ConnectionString).Options);

    static Task<int> CountShadowColumns(DbContext context) => context.Database.GetDatabaseType() switch
    {
        DatabaseType.PostgreSql => context.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM information_schema.columns WHERE table_schema=current_schema() AND table_name={Table} AND column_name='__compositeKey' AND is_nullable='YES' AND column_default IS NULL").SingleAsync(),
        DatabaseType.SqlServer => context.Database.SqlQuery<int>($"SELECT count(*) AS [Value] FROM sys.columns WHERE object_id=OBJECT_ID({Table}) AND name='__compositeKey' AND is_nullable=1 AND default_object_id=0").SingleAsync(),
        _ => context.Database.SqlQuery<int>($"SELECT count(*) AS Value FROM pragma_table_xinfo({Table}) WHERE name='__compositeKey' AND \"notnull\"=0 AND dflt_value IS NULL AND hidden=0").SingleAsync()
    };

    static Task<int> CountShadowIndexes(DbContext context) => context.Database.GetDatabaseType() switch
    {
        DatabaseType.PostgreSql => context.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM pg_indexes WHERE schemaname=current_schema() AND tablename={Table} AND indexname LIKE 'chronicle_shx_%'").SingleAsync(),
        DatabaseType.SqlServer => context.Database.SqlQuery<int>($"SELECT count(*) AS [Value] FROM sys.indexes WHERE object_id=OBJECT_ID({Table}) AND name LIKE 'chronicle_shx_%'").SingleAsync(),
        _ => context.Database.SqlQuery<int>($"SELECT count(*) AS Value FROM pragma_index_list({Table}) WHERE name LIKE 'chronicle_shx_%'").SingleAsync()
    };

    void Destroy() => _harness.Dispose();

    protected sealed class StoredRow
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Stamp { get; set; }
    }
}
