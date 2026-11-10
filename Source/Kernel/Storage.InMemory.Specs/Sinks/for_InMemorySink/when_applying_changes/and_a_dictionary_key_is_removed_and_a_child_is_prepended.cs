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

public class and_a_dictionary_key_is_removed_and_a_child_is_prepended : Specification
{
    InMemorySink _sink;
    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    Key _key;
    ExpandoObject? _result;

    void Establish()
    {
        _key = new Key("user-1", ArrayIndexers.NoIndexers);
        _sink = new InMemorySink(CreateReadModelDefinition(), new TypeFormats());

        var initialState = new ExpandoObject();
        var slots = new ExpandoObject();
        ((IDictionary<string, object?>)slots)["first"] = "series-1";
        ((IDictionary<string, object?>)slots)["second"] = "series-2";
        ((IDictionary<string, object?>)initialState)["slots"] = slots;
        ((IDictionary<string, object?>)initialState)["roles"] = new List<object> { CreateRole("role-1") };

        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.InitialState.Returns(initialState);
        _changeset.Changes.Returns([
            new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [PropertyDifference.Removal(new PropertyPath("slots.first"), "series-1")]),
            new ChildAdded(CreateRole("role-2"), new PropertyPath("roles"), new PropertyPath("id"), "role-2", ArrayIndexers.NoIndexers, new(ChildrenPlacement.Prepend, PropertyPath.NotSet))
        ]);
    }

    async Task Because()
    {
        await _sink.ApplyChanges(_key, _changeset, 42UL);
        _result = await _sink.FindOrDefault(_key);
    }

    IDictionary<string, object?> Slots => (IDictionary<string, object?>)((IDictionary<string, object?>)_result!)["slots"]!;

    [Fact] void should_remove_the_key_rather_than_store_null() => Slots.ContainsKey("first").ShouldBeFalse();
    [Fact] void should_keep_the_other_key() => Slots["second"].ShouldEqual("series-2");
    [Fact] void should_insert_the_prepended_child_first() => string.Join(',', GetRoleIds()).ShouldEqual("role-2,role-1");

    IEnumerable<string> GetRoleIds()
    {
        var dictionary = (IDictionary<string, object?>)_result!;
        return ((IEnumerable<object>)dictionary["roles"]!).Cast<IDictionary<string, object?>>().Select(_ => (string)_["id"]!);
    }

    static ExpandoObject CreateRole(string id)
    {
        var item = new ExpandoObject();
        ((IDictionary<string, object?>)item)["id"] = id;
        return item;
    }

    static ReadModelDefinition CreateReadModelDefinition() =>
        new(
            "test-read-model",
            "TestReadModel",
            "TestReadModel",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                { ReadModelGeneration.First, new JsonSchema() }
            },
            []);
}
