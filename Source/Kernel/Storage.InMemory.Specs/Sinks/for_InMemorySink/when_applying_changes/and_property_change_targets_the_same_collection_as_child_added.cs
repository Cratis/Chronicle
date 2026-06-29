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

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_applying_changes;

public class and_property_change_targets_the_same_collection_as_child_added : Specification
{
    InMemorySink _sink;
    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    Key _key;
    ExpandoObject? _result;
    IEnumerable<FailedPartition> _failedPartitions;

    void Establish()
    {
        _key = new Key("user-1", ArrayIndexers.NoIndexers);
        _sink = new InMemorySink(CreateReadModelDefinition(), new TypeFormats());

        var role = CreateRole("Administrator");
        var rolesProperty = new PropertyPath("roles");
        var childAdded = new ChildAdded(role, rolesProperty, new PropertyPath("role"), "Administrator", ArrayIndexers.NoIndexers);

        dynamic state = new ExpandoObject();
        state.name = "Ada Lovelace";
        state.roles = new[] { role };

        PropertyDifference[] differences =
        [
            new PropertyDifference(new PropertyPath("name"), null, "Ada Lovelace"),
            new PropertyDifference(rolesProperty, Array.Empty<object>(), new[] { role })
        ];
        var propertiesChanged = new PropertiesChanged<ExpandoObject>(state, differences);

        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.InitialState.Returns(new ExpandoObject());
        Change[] changes = [propertiesChanged, childAdded];
        _changeset.Changes.Returns(changes);
    }

    async Task Because()
    {
        _failedPartitions = await _sink.ApplyChanges(_key, _changeset, 42UL);
        _result = await _sink.FindOrDefault(_key);
    }

    [Fact] void should_not_fail() => _failedPartitions.ShouldBeEmpty();
    [Fact] void should_keep_non_conflicting_property_changes() => GetValue<string>("name").ShouldEqual("Ada Lovelace");
    [Fact] void should_apply_the_child_operation_once() => GetRoles().Length.ShouldEqual(1);
    [Fact] void should_keep_the_added_role() => GetValue<string>(GetRoles()[0], "role").ShouldEqual("Administrator");

    T GetValue<T>(string property) => GetValue<T>(_result!, property);

    static T GetValue<T>(ExpandoObject target, string property)
    {
        var dictionary = (IDictionary<string, object?>)target;
        return (T)dictionary[property]!;
    }

    ExpandoObject[] GetRoles()
    {
        var dictionary = (IDictionary<string, object?>)_result!;
        return ((IEnumerable<object>)dictionary["roles"]).Cast<ExpandoObject>().ToArray();
    }

    static ExpandoObject CreateRole(string role)
    {
        dynamic item = new ExpandoObject();
        item.role = role;
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
