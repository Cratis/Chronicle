// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelMigrator.when_ensuring_table_is_migrated;

public class and_child_only_reads_keep_the_marker_index : Specification
{
    SqlSinkHarness _harness;
    ISink _sink;
    string[] _before;
    string[] _after;

    async Task Establish()
    {
        _harness = new SqlSinkHarness();
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"}}}""");
        _sink = _harness.CreateSink(new ReadModelDefinition("child-only", "child_only", "Child only", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "child-only", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []));
        await _sink.GetInstances();
        _before = await MarkerIndexes();
    }

    async Task Because()
    {
        await _sink.GetInstances(null, 0, 50, ReadModelInstanceVisibility.IncludeUninitialized);
        _after = await MarkerIndexes();
    }

    void Destroy() => _harness.Dispose();

    async Task<string[]> MarkerIndexes()
    {
        await using var context = new DbContext(new DbContextOptionsBuilder().UseSqlite(_harness.ConnectionString).Options);
        return await context.Database.SqlQuery<string>($"SELECT name AS Value FROM sqlite_master WHERE type='index' AND tbl_name='child_only' AND substr(name,1,14)={InitializedMarkerIndex.Prefix}").ToArrayAsync();
    }

    [Fact] void should_have_a_marker_index() => _before.Length.ShouldEqual(1);
    [Fact] void should_keep_the_marker_index() => _after.ShouldEqual(_before);
}
