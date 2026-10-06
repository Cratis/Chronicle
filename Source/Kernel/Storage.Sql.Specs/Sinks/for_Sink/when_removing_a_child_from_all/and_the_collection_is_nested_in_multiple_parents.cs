// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_removing_a_child_from_all;

public class and_the_collection_is_nested_in_multiple_parents : Specification
{
    SqlSinkHarness _harness;
    ISink _sink;
    IDictionary<string, object?>[] _stored;

    async Task Establish()
    {
        var schema = await JsonSchema.FromJsonAsync("""
            { "type": "object", "properties": {
              "id": { "type": "string" },
              "parents": { "type": "array", "items": { "type": "object", "properties": {
                "id": { "type": "string" }, "items": { "type": "array", "items": { "type": "object", "properties": { "id": { "type": "string" } } } }
              } } }
            } }
            """);
        _harness = new SqlSinkHarness();
        _sink = _harness.CreateSink(new ReadModelDefinition(
            "orders",
            "orders",
            "Orders",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            "orders",
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema },
            []));
        foreach (var key in (string[])["first", "second"])
        {
            var parents = new List<ExpandoObject>
            {
                Object(("id", "item"), ("items", new List<ExpandoObject> { Object(("id", "item")), Object(("id", "keep")) })),
                Object(("id", "parent-2"), ("items", new List<ExpandoObject> { Object(("id", "item")) })),
                Object(("id", "without-items"))
            };
            var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
            changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("parents", null, parents)])]);
            await _sink.ApplyChanges(new Key(key, ArrayIndexers.NoIndexers), changeset, 1UL);
        }
    }

    async Task Because()
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.Changes.Returns([new ChildRemovedFromAll("[parents].[items]", "id", "item", ArrayIndexers.NoIndexers)]);
        await _sink.ApplyChanges(new Key("first", ArrayIndexers.NoIndexers), changeset, 2UL);
        _stored = [(await _sink.FindOrDefault(new Key("first", ArrayIndexers.NoIndexers)))!, (await _sink.FindOrDefault(new Key("second", ArrayIndexers.NoIndexers)))!];
    }

    [Fact] void should_preserve_all_parent_elements_in_both_documents() => _stored.Select(row => Parents(row).Count()).ShouldEqual<IEnumerable<int>>([3, 3]);
    [Fact] void should_remove_only_the_matching_nested_children() => _stored.SelectMany(Parents).Where(parent => parent.ContainsKey("items")).Select(parent => ((IEnumerable<object>)parent["items"]!).Count()).ShouldEqual<IEnumerable<int>>([1, 0, 1, 0]);
    [Fact] void should_preserve_unmatched_nested_children() => _stored.SelectMany(Parents).Where(parent => parent.ContainsKey("items")).SelectMany(parent => (IEnumerable<object>)parent["items"]!).Cast<IDictionary<string, object?>>().Select(child => child["id"]).ShouldEqual<IEnumerable<object?>>(["keep", "keep"]);

    void Destroy() => _harness.Dispose();

    static IEnumerable<IDictionary<string, object?>> Parents(IDictionary<string, object?> row) => ((IEnumerable<object>)row["parents"]!).Cast<IDictionary<string, object?>>();

    static ExpandoObject Object(params (string Name, object Value)[] values)
    {
        var result = new ExpandoObject();
        foreach (var (name, value) in values)
        {
            ((IDictionary<string, object?>)result)[name] = value;
        }
        return result;
    }
}
