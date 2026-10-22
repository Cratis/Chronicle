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
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ensuring_indexes;

public abstract class when_reconciliation_follows_an_active_builder : Specification
{
    ISqlSinkHarness _harness;
    readonly OnlineIndexGate _gate = new();
    Task _build = Task.CompletedTask;
    Task _reconciliation = Task.CompletedTask;
    List<string> _names;
    string _expected;
    string _obsolete;
    protected abstract ISqlSinkHarness CreateHarness(OnlineIndexGate gate);

    async Task Because()
    {
        _harness = CreateHarness(_gate);
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"},"old":{"type":"integer"},"current":{"type":"integer"},"last":{"type":"integer"}}}""");
        var definition = new ReadModelDefinition("queued-index", "queued_index", "Indexes", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "indexes", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, [new IndexDefinition("old")]);
        var initial = _harness.CreateSink(definition);
        await initial.EnsureIndexes();
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("old", null, 1), new PropertyDifference("current", null, 2), new PropertyDifference("last", null, 3)])]);
        await initial.ApplyChanges(new Key("row", ArrayIndexers.NoIndexers), changeset, 1UL);
        var building = (Sink)_harness.CreateSink(definition with { Indexes = [new IndexDefinition("current")] });
        await building.EnsureIndexes();
        _build = building.IndexBuildCompletion;
        try
        {
            await _gate.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var latest = (Sink)_harness.CreateSink(definition with { Indexes = [new IndexDefinition("last")] });
            await latest.ReconcileIndexes();
            _reconciliation = latest.IndexBuildCompletion;
        }
        finally
        {
            _gate.Continue.TrySetResult();
        }
        await _build;
        await _reconciliation;
        await using var context = new DbContext(new DbContextOptionsBuilder<DbContext>().UseDatabaseFromConnectionString(_harness.ConnectionString).Options);
        var table = definition.ContainerName.Value;
        _names = context.Database.IsNpgsql()
            ? await context.Database.SqlQuery<string>($"SELECT indexname AS \"Value\" FROM pg_indexes WHERE schemaname=current_schema() AND tablename={table}").ToListAsync()
            : await context.Database.SqlQuery<string>($"SELECT name AS [Value] FROM sys.indexes WHERE object_id=OBJECT_ID({table}) AND name IS NOT NULL").ToListAsync();
        _expected = ReadModelIndexNames.For(context.Database.GetDatabaseType(), "queued_index", "last");
        _obsolete = ReadModelIndexNames.For(context.Database.GetDatabaseType(), "queued_index", "current");
    }

    async Task Destroy()
    {
        _gate.Continue.TrySetResult();
        await _build;
        await _reconciliation;
        _harness.Dispose();
    }

    [Fact] public void should_create_the_latest_declared_index_after_the_active_builder_finishes() => _names.ShouldContain(_expected);
    [Fact] public void should_prune_the_index_created_by_the_older_declaration() => _names.ShouldNotContain(_obsolete);
}
