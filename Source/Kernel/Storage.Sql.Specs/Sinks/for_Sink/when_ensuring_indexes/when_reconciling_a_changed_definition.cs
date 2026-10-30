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
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ensuring_indexes;

public abstract class when_reconciling_a_changed_definition : Specification
{
    ISqlSinkHarness _harness;
    bool _retainedOnActivation;
    bool _removedOnChange;
    bool _createdReplacement;
    bool _preservedCustom;
    int _rows;
    bool _availableDuringBuild;
    bool _competingBuilderSkipped;
    bool _crossKernelSkipped;
    readonly OnlineIndexGate _gate = new();
    Task _background = Task.CompletedTask;
    protected abstract ISqlSinkHarness CreateHarness(OnlineIndexGate gate);

    async Task Because()
    {
        _harness = CreateHarness(_gate);
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"},"old":{"type":"integer"},"current":{"type":"integer"}}}""");
        var definition = new ReadModelDefinition("index-lifecycle", "index_lifecycle", "Indexes", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "indexes", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, [new IndexDefinition("old")]);
        var original = (Sink)_harness.CreateSink(definition);
        await original.EnsureIndexes();
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("old", null, 1), new PropertyDifference("current", null, 2)])]);
        await original.ApplyChanges(new Key("row", ArrayIndexers.NoIndexers), changeset, 1UL);
        var options = SpecDbContextOptions.ForConnection<DbContext>(_harness.ConnectionString);
        await using var context = new DbContext(options);
        var helper = context.GetService<ISqlGenerationHelper>();
#pragma warning disable EF1002 // Test identifiers are delimited by the provider helper.
        await context.Database.ExecuteSqlRawAsync($"CREATE INDEX manual_index ON {helper.DelimitIdentifier("index_lifecycle")} ({helper.DelimitIdentifier("old")})");
#pragma warning restore EF1002
        var changed = definition with { Indexes = [new IndexDefinition("current")] };
        await context.Database.OpenConnectionAsync();
        var connection = context.Database.GetDbConnection();
        var schemaLock = DatabaseSchemaLock.For(connection);
        if (schemaLock is not null)
        {
            await schemaLock.Acquire(connection, "chronicle:read-model-indexes:index_lifecycle", TimeSpan.Zero);
            try
            {
                var blocked = (Sink)_harness.CreateSink(changed);
                await blocked.EnsureIndexes();
                await blocked.IndexBuildCompletion.WaitAsync(TimeSpan.FromSeconds(10));
                _crossKernelSkipped = !(await Names(context)).Contains(ReadModelIndexNames.For(context.Database.GetDatabaseType(), "index_lifecycle", "current"));
            }
            finally
            {
                await schemaLock.Release(connection, "chronicle:read-model-indexes:index_lifecycle");
            }
        }
        else
        {
            _crossKernelSkipped = true;
        }
        var activation = (Sink)_harness.CreateSink(changed);
        await activation.EnsureIndexes();
        _background = activation.IndexBuildCompletion;
        var type = context.Database.GetDatabaseType();
        if (type != DatabaseType.Sqlite)
        {
            try
            {
                await _gate.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
                _availableDuringBuild = (await activation.GetInstances().WaitAsync(TimeSpan.FromSeconds(10))).TotalCount == 1;
                var competitor = (Sink)_harness.CreateSink(changed);
                await competitor.EnsureIndexes();
                await competitor.IndexBuildCompletion.WaitAsync(TimeSpan.FromSeconds(10));
                _competingBuilderSkipped = true;
            }
            finally
            {
                _gate.Continue.TrySetResult();
            }
        }
        else
        {
            _availableDuringBuild = true;
            _competingBuilderSkipped = true;
        }
        await _background;
        _retainedOnActivation = (await Names(context)).Contains(ReadModelIndexNames.For(type, "index_lifecycle", "old"));
        var reconciliation = (Sink)_harness.CreateSink(changed);
        await reconciliation.ReconcileIndexes();
        await reconciliation.IndexBuildCompletion;
        var names = await Names(context);
        _removedOnChange = !names.Contains(ReadModelIndexNames.For(type, "index_lifecycle", "old"));
        _createdReplacement = names.Contains(ReadModelIndexNames.For(type, "index_lifecycle", "current"));
        _preservedCustom = names.Contains("manual_index");
        _rows = (await reconciliation.GetInstances()).Instances.Count();
    }

    static async Task<List<string>> Names(DbContext context, string table = "index_lifecycle") => context.Database.GetDatabaseType() switch
    {
        DatabaseType.PostgreSql => await context.Database.SqlQuery<string>($"SELECT indexname AS \"Value\" FROM pg_indexes WHERE schemaname=current_schema() AND tablename={table}").ToListAsync(),
        DatabaseType.SqlServer => await context.Database.SqlQuery<string>($"SELECT name AS [Value] FROM sys.indexes WHERE object_id=OBJECT_ID({table}) AND name IS NOT NULL").ToListAsync(),
        _ => await context.Database.SqlQuery<string>($"SELECT name AS Value FROM sqlite_master WHERE type='index' AND tbl_name={table}").ToListAsync()
    };

    async Task Destroy()
    {
        _gate.Continue.TrySetResult();
        await _background;
        _harness.Dispose();
    }
    [Fact] public void should_not_prune_during_routine_activation() => _retainedOnActivation.ShouldBeTrue();
    [Fact] public void should_remove_undeclared_chronicle_indexes_on_definition_change() => _removedOnChange.ShouldBeTrue();
    [Fact] public void should_create_the_replacement_index() => _createdReplacement.ShouldBeTrue();
    [Fact] public void should_preserve_nonchronicle_indexes() => _preservedCustom.ShouldBeTrue();
    [Fact] public void should_keep_primary_rows_available_during_online_creation() => _availableDuringBuild.ShouldBeTrue();
    [Fact] public void should_skip_when_another_kernel_owns_the_table_lock() => _crossKernelSkipped.ShouldBeTrue();
    [Fact] public void should_skip_a_competing_builder_without_waiting() => _competingBuilderSkipped.ShouldBeTrue();
    [Fact] public void should_preserve_the_rows() => _rows.ShouldEqual(1);
}
