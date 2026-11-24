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

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_removing_a_child;

public class and_the_composite_key_has_materialized_leaves : Sinks.given.materialized_children
{
    readonly SqlSinkHarness _harness = new();
    ISink _sink;
    string[] _remaining;

    async Task Establish()
    {
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"children":{"type":"array","items":{"type":"object","properties":{"id":{"type":"object","properties":{"tenant":{"type":"string"},"number":{"type":"integer"},"id":{"type":"string"}}},"name":{"type":"string"}}}}}}
            """);
        var definition = new ReadModelDefinition("materialized-children", "MaterializedChildren", "MaterializedChildren", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []);
        _sink = _harness.CreateSink(definition);
        for (var index = 0; index < _children.Count; index++)
        {
            ((IDictionary<string, object?>)_children[index]!)["name"] = index.ToString();
        }
        await Apply(new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("children", null, _children)]), 1);
    }

    async Task Because()
    {
        await Apply(new ChildRemoved(_target, "[children]", "id", _key), 2);
        var stored = (await _sink.FindOrDefault(new Key("document", ArrayIndexers.NoIndexers)))!;
        _remaining = ((IEnumerable<object>)((IDictionary<string, object?>)stored)["children"]!).Cast<IDictionary<string, object?>>().Select(child => child["name"]!.ToString()!).ToArray();
    }

    void Destroy() => _harness.Dispose();

    [Fact] void should_remove_only_the_materialized_target_in_sqlite() => _remaining.ShouldEqual("0", "1", "3");

    Task Apply(Change change, ulong sequence)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.Changes.Returns(new[] { change });
        return _sink.ApplyChanges(new Key("document", ArrayIndexers.NoIndexers), changeset, sequence);
    }
}
