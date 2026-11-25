// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_converting_to_update_definition;

public class and_a_joined_matches_a_child_added_in_the_same_changeset : given.a_converter_rendering_updates
{
    ExpandoObject _child;

    void Establish()
    {
        _child = new ExpandoObject();
        _expandoObjectConverter.ToBsonDocument(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(call => new BsonDocument(call.Arg<ExpandoObject>().ToDictionary(_ => _.Key, _ => BsonValue.Create(_.Value))));
    }

    Task Because()
    {
        var indexers = new ArrayIndexers([new ArrayIndexer(new PropertyPath("Children"), new PropertyPath("Id"), "child-key")]);
        var childAdded = new ChildAdded(_child, new PropertyPath("Children"), new PropertyPath("Id"), "child-key", ArrayIndexers.NoIndexers);
        var joined = new Joined(
            new ExpandoObject(),
            "child-key",
            new PropertyPath("Id"),
            indexers,
            [new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference(new PropertyPath("Name"), null, "Joined name", indexers)])]);
        return Convert(new Key("key", ArrayIndexers.NoIndexers), 42UL, joined, childAdded);
    }

    [Fact] void should_merge_the_joined_property_into_the_pushed_child() => _update["$push"]["Children"]["Name"].ShouldEqual(new BsonString("Joined name"));
    [Fact] void should_not_update_joined_documents() => _joinUpdates.ShouldBeEmpty();
}
