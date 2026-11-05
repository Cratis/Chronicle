// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink;

public abstract class when_object_columns_are_replaced : Specification
{
    const string Container = "object_replacements";
    readonly Key _key = new("root", ArrayIndexers.NoIndexers);
    ISqlSinkHarness _harness;
    ISink _sink;
    bool _nullColumn;
    object? _nullRead;
    string _recreatedColumn;
    IDictionary<string, object?> _recreatedRead;
    string _emptyColumn;
    IDictionary<string, object?> _emptyRead;
    bool _replayedNullColumn;
    object? _replayedRead;

    protected abstract bool UseIsolatedReplace { get; }

    protected abstract ISqlSinkHarness CreateHarness();

    async Task Establish()
    {
        _harness = CreateHarness();
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"},"child":{"type":"object","additionalProperties":{}}}}""");
        _sink = _harness.CreateSink(new ReadModelDefinition(
            "object-replacements",
            Container,
            "Object replacements",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema },
            []));
        dynamic child = new ExpandoObject();
        child.old = "before";
        await WriteObject((ExpandoObject)child, 1UL);
    }

    async Task Because()
    {
        await WriteObject(null, 2UL);
        _nullColumn = await ColumnIsNull();
        _nullRead = await ReadObject();

        if (UseIsolatedReplace)
        {
            dynamic child = new ExpandoObject();
            child.name = "recreated";
            await WriteObject((ExpandoObject)child, 3UL);
        }
        else
        {
            await _sink.ApplyChanges(_key, Changeset("child.name", "recreated"), 3UL);
        }
        _recreatedColumn = await ReadColumn();
        _recreatedRead = (IDictionary<string, object?>)(await ReadObject())!;

        await WriteObject(new ExpandoObject(), 4UL);
        _emptyColumn = await ReadColumn();
        _emptyRead = (IDictionary<string, object?>)(await ReadObject())!;

        var replay = new ReplayContext(new ReadModelType("object-replacements", ReadModelGeneration.First), Container, $"{Container}-revert", DateTimeOffset.UtcNow);
        await _sink.BeginReplay(replay);
        await WriteObject(null, 5UL);
        (await _sink.EndReplay(replay)).ShouldBeEmpty();
        _replayedNullColumn = await ColumnIsNull();
        _replayedRead = await ReadObject();
    }

    void Destroy() => _harness.Dispose();

    [Fact] public void should_store_sql_null_for_an_explicit_null() => _nullColumn.ShouldBeTrue();
    [Fact] public void should_read_the_cleared_object_as_null() => _nullRead.ShouldBeNull();
    [Fact] public void should_persist_the_recreated_object() => JsonNode.Parse(_recreatedColumn)!["name"]!.GetValue<string>().ShouldEqual("recreated");
    [Fact] public void should_read_the_recreated_object() => _recreatedRead["name"].ShouldEqual("recreated");
    [Fact] public void should_not_restore_old_properties() => _recreatedRead.ContainsKey("old").ShouldBeFalse();
    [Fact] public void should_store_a_genuine_empty_object() => JsonNode.Parse(_emptyColumn)!.AsObject().Count.ShouldEqual(0);
    [Fact] public void should_read_a_genuine_empty_object() => _emptyRead.Count.ShouldEqual(0);
    [Fact] public void should_reproject_an_explicit_null_as_sql_null() => _replayedNullColumn.ShouldBeTrue();
    [Fact] public void should_read_the_reprojected_object_as_null() => _replayedRead.ShouldBeNull();

    async Task WriteObject(object? value, EventSequenceNumber sequence)
    {
        if (UseIsolatedReplace)
        {
            var state = new ExpandoObject();
            ((IDictionary<string, object?>)state)["child"] = value;
            (await _sink.ReplaceIsolated(_key, state, sequence)).ShouldEqual(IsolatedReplaceResult.Replaced);
        }
        else
        {
            (await _sink.ApplyChanges(_key, Changeset("child", value), sequence)).ShouldBeEmpty();
        }
    }

    static IChangeset<AppendedEvent, ExpandoObject> Changeset(string path, object? value)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference(path, null, value)])]);
        return changeset;
    }

    async Task<object?> ReadObject()
    {
        var row = (IDictionary<string, object?>)(await _sink.FindOrDefault(_key))!;
        row["id"].ShouldEqual("root");
        return row.TryGetValue("child", out var child) ? child : null;
    }

    async Task<bool> ColumnIsNull() => Convert.ToInt32(await QueryColumn(testNull: true), System.Globalization.CultureInfo.InvariantCulture) == 1;

    async Task<string> ReadColumn() => (string)(await QueryColumn(testNull: false))!;

    async Task<object?> QueryColumn(bool testNull)
    {
        await using var context = new DbContext(SpecDbContextOptions.ForConnection<DbContext>(_harness.ConnectionString));
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        if (testNull)
        {
            command.CommandText = "SELECT CASE WHEN child IS NULL THEN 1 ELSE 0 END FROM object_replacements WHERE id = 'root'";
        }
        else
        {
            command.CommandText = "SELECT child FROM object_replacements WHERE id = 'root'";
        }
        command.CommandTimeout = 30;
        return await command.ExecuteScalarAsync();
    }
}
