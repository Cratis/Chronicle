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

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_applying_changes.given;

public class a_sink_with_a_child_join : Specification
{
    protected InMemorySink _sink;
    protected IChangeset<AppendedEvent, ExpandoObject> _changeset;
    protected Key _key;

    void Establish()
    {
        var readModel = new ReadModelDefinition(
            "test",
            "test",
            "test",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = new JsonSchema() },
            []);
        _sink = new InMemorySink(readModel, new TypeFormats());
        _key = new Key("root", ArrayIndexers.NoIndexers);
        StoreRoot("root");

        var initial = new ExpandoObject();
        var indexers = new ArrayIndexers([new ArrayIndexer("[children]", "childId", "child")]);
        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.InitialState.Returns(initial);
        _changeset.Changes.Returns(
        [
            new Joined(
                initial,
                "child",
                "[children].childId",
                indexers,
                [new PropertiesChanged<ExpandoObject>(initial, [new PropertyDifference("[children].name", "before", "after", indexers)])])
        ]);
    }

    protected void StoreRoot(string id)
    {
        dynamic child = new ExpandoObject();
        child.childId = "child";
        child.name = "before";
        child.extra = "preserved";
        dynamic sibling = new ExpandoObject();
        sibling.childId = "sibling";
        sibling.name = "sibling name";
        dynamic root = new ExpandoObject();
        root.id = id;
        root.title = "root title";
        root.__initialized = true;
        root.children = new List<object> { child, sibling };
        _sink.Collection[id] = root;
    }

    protected IDictionary<string, object?> Root(string id) => _sink.Collection[id];
    protected IDictionary<string, object?> Child(string id) => Children(id)[0];
    protected IDictionary<string, object?>[] Children(string id) => ((IEnumerable<object>)Root(id)["children"]!).Cast<IDictionary<string, object?>>().ToArray();
}
