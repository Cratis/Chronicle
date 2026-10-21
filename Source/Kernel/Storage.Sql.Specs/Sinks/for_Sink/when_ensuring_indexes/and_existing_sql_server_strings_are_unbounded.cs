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
using Cratis.Chronicle.Storage.ReadModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ensuring_indexes;

[Collection(SqlServerCollection.Name)]
public class and_existing_sql_server_strings_are_unbounded(SqlServerFixture fixture) : Specification
{
    SqlServerSinkHarness _harness;
    Sink _sink;
    int _existingLength;
    int _declaredLength;
    int _replayedLength;
    int _indexCount;
    Exception _writeError;

    async Task Establish()
    {
        _harness = new SqlServerSinkHarness { Fixture = fixture };
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"},"name":{"type":"string"},"bounded":{"type":"string","maxLength":80}}}""");
        var definition = new ReadModelDefinition("strings", "bounded_strings", "Strings", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "strings", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, [new IndexDefinition("name"), new IndexDefinition("bounded")]);
        _sink = (Sink)_harness.CreateSink(definition);
        var options = new DbContextOptionsBuilder<DbContext>().UseDatabaseFromConnectionString(_harness.ConnectionString).Options;
        await using var context = new DbContext(options);
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE bounded_strings (id NVARCHAR(200) PRIMARY KEY, name NVARCHAR(MAX))");
    }

    async Task Because()
    {
        await _sink.EnsureIndexes();
        var options = new DbContextOptionsBuilder<DbContext>().UseDatabaseFromConnectionString(_harness.ConnectionString).Options;
        await using var context = new DbContext(options);
        _existingLength = await Length(context, "bounded_strings", "name");
        _declaredLength = await Length(context, "bounded_strings", "bounded");
        _indexCount = await context.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM sys.indexes WHERE object_id=OBJECT_ID('bounded_strings') AND name LIKE 'chronicle_idx_%'").SingleAsync();
        await _sink.BeginReplay(new ReplayContext(new ReadModelType("strings", ReadModelGeneration.First), "bounded_strings", "bounded_strings-revert", DateTimeOffset.UtcNow));
        _replayedLength = await Length(context, "replay-bounded_strings", "name");
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("name", null, new string('x', 451)), new PropertyDifference("bounded", null, "short")])]);
        try
        {
            await _sink.ApplyChanges(new Key("row", ArrayIndexers.NoIndexers), changeset, 1UL);
        }
        catch (DbUpdateException exception)
        {
            _writeError = exception.InnerException;
        }
    }

    static Task<int> Length(DbContext context, string table, string column) => context.Database.SqlQuery<int>($"SELECT CAST(CHARACTER_MAXIMUM_LENGTH AS int) AS [Value] FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=SCHEMA_NAME() AND TABLE_NAME={table} AND COLUMN_NAME={column}").SingleAsync();

    void Destroy() => _harness.Dispose();

    [Fact] void should_not_alter_an_existing_column() => _existingLength.ShouldEqual(-1);
    [Fact] void should_apply_an_explicit_bound_to_a_new_column() => _declaredLength.ShouldEqual(80);
    [Fact] void should_skip_the_unbounded_index_but_create_the_bounded_index() => _indexCount.ShouldEqual(1);
    [Fact] void should_apply_the_default_indexed_bound_on_replay() => _replayedLength.ShouldEqual(450);
    [Fact] void should_fail_loudly_without_truncating() => new[] { 2628, 8152 }.ShouldContain(((SqlException)_writeError).Number);
}
