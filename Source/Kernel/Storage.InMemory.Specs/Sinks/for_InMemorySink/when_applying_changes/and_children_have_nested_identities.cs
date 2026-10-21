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

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_applying_changes;

public class and_children_have_nested_identities : Specification
{
    readonly Key _key = new("parent", ArrayIndexers.NoIndexers);
    InMemorySink _sink;
    ExpandoObject _updated;
    ExpandoObject _removed;

    async Task Establish()
    {
        var definition = new ReadModelDefinition("nested-identities", "NestedIdentities", "NestedIdentities", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = new() }, []);
        _sink = new InMemorySink(definition, new TypeFormats());
        await Apply(new ChildAdded(Child("A", "First"), "[children]", "identity.id", "A", ArrayIndexers.NoIndexers), 1);
        await Apply(new ChildAdded(Child("B", "Second"), "[children]", "identity.id", "B", ArrayIndexers.NoIndexers), 2);
    }

    async Task Because()
    {
        var indexers = new ArrayIndexers([new ArrayIndexer("[children]", "identity.id", "A")]);
        await Apply(new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("[children].identity.topic", "First", "Updated", indexers)]), 3);
        _updated = (await _sink.FindOrDefault(_key))!;
        await Apply(new ChildRemoved(Child("A", "Updated"), "[children]", "identity.id", "A"), 4);
        _removed = (await _sink.FindOrDefault(_key))!;
    }

    [Fact] void should_update_without_adding_a_child() => Children(_updated).Length.ShouldEqual(2);
    [Fact] void should_update_the_matching_child() => new PropertyPath("identity.topic").GetValue(Children(_updated)[0], ArrayIndexers.NoIndexers).ShouldEqual("Updated");
    [Fact] void should_remove_the_matching_child() => Children(_removed).Length.ShouldEqual(1);
    [Fact] void should_preserve_the_other_child() => new PropertyPath("identity.id").GetValue(Children(_removed)[0], ArrayIndexers.NoIndexers).ShouldEqual("B");

    async Task Apply(Change change, ulong sequenceNumber)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(await _sink.FindOrDefault(_key) ?? new ExpandoObject());
        changeset.Changes.Returns([change]);
        (await _sink.ApplyChanges(_key, changeset, sequenceNumber)).ShouldBeEmpty();
    }

    static ExpandoObject Child(string id, string topic)
    {
        dynamic child = new ExpandoObject();
        child.identity = new ExpandoObject();
        child.identity.id = id;
        child.identity.topic = topic;
        return child;
    }

    static ExpandoObject[] Children(ExpandoObject state) => ((IEnumerable<object>)((IDictionary<string, object?>)state)["children"]!).Cast<ExpandoObject>().ToArray();
}
