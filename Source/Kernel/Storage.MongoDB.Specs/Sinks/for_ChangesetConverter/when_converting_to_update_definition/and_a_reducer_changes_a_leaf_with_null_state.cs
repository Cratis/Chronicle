// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_converting_to_update_definition;

public class and_a_reducer_changes_a_leaf_with_null_state : given.a_changeset_converter
{
    BsonDocument _update;
    IReadOnlyList<string> _parents;

    void Establish()
    {
        _mongoDBConverter.ToMongoDBProperty(Arg.Any<PropertyPath>(), Arg.Any<ArrayIndexers>())
            .Returns(call => new MongoDBProperty(((PropertyPath)call[0]).Path, []));
        _mongoDBConverter.ToBsonValue(Arg.Any<object?>(), Arg.Any<PropertyPath>()).Returns(new BsonString("ciphertext"));
    }

    async Task Because()
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(null!, [new PropertyDifference("outer.info.name", null, "ciphertext")])]);
        var result = await _converter.ToUpdateDefinition(new Key("key", ArrayIndexers.NoIndexers), changeset, EventSequenceNumber.Unavailable);
        _update = result.UpdateDefinition.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry)).AsBsonDocument;
        _parents = result.NullParentPaths;
    }

    [Fact] void should_set_only_the_leaf() => _update["$set"]["outer.info.name"].AsString.ShouldEqual("ciphertext");
    [Fact] void should_prepare_outermost_first() => _parents.SequenceEqual(["outer", "outer.info"]).ShouldBeTrue();
}
