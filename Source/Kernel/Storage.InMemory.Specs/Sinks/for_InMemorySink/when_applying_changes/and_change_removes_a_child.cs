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

public class and_change_removes_a_child : Specification
{
    const string RemovedRoleId = "role-1";
    const string KeptRoleId = "role-2";

    InMemorySink _sink;
    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    Key _key;
    ExpandoObject? _result;

    void Establish()
    {
        _key = new Key("user-1", ArrayIndexers.NoIndexers);
        _sink = new InMemorySink(CreateReadModelDefinition(), new TypeFormats());

        dynamic initialState = new ExpandoObject();
        initialState.roles = new[] { CreateRole(RemovedRoleId), CreateRole(KeptRoleId) };

        var childRemoved = new ChildRemoved(CreateRole(RemovedRoleId), new PropertyPath("roles"), new PropertyPath("id"), RemovedRoleId);

        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.InitialState.Returns((ExpandoObject)initialState);
        _changeset.Changes.Returns([childRemoved]);
    }

    async Task Because()
    {
        await _sink.ApplyChanges(_key, _changeset, 42UL);
        _result = await _sink.FindOrDefault(_key);
    }

    [Fact] void should_have_one_role_left() => GetRoles().Length.ShouldEqual(1);
    [Fact] void should_keep_the_other_role() => GetValue<string>(GetRoles()[0], "id").ShouldEqual(KeptRoleId);

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

    static ExpandoObject CreateRole(string id)
    {
        dynamic item = new ExpandoObject();
        item.id = id;
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
