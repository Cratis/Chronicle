// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_converting_to_update_definition;

public class and_a_parent_and_its_leaf_both_change : given.a_changeset_converter
{
    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    BsonDocument _rendered;

    void Establish()
    {
        dynamic initial = new ExpandoObject();
        initial.info = new ExpandoObject();
        initial.info.name = "First";
        dynamic changed = new ExpandoObject();
        changed.info = new ExpandoObject();
        changed.info.name = "Again";
        var parent = new PropertyPath("info");
        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.InitialState.Returns((ExpandoObject)initial);
        _changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>((ExpandoObject)changed,
            [new PropertyDifference(new PropertyPath("info.name"), "First", "Again"),
             new PropertyDifference(parent, (ExpandoObject)initial.info, (ExpandoObject)changed.info)])]);
        _mongoDBConverter.ToMongoDBProperty(parent, ArrayIndexers.NoIndexers).Returns(new MongoDBProperty("info", []));
        _mongoDBConverter.ToBsonValue(Arg.Any<object?>(), parent).Returns(new BsonDocument("name", "Again"));
        _mongoDBConverter.ToBsonValue(Arg.Any<EventSequenceNumber>()).Returns(BsonValue.Create(42UL));
    }

    async Task Because()
    {
        var result = await _converter.ToUpdateDefinition(new Key("probe-1", ArrayIndexers.NoIndexers), _changeset, 42UL);
        _rendered = result.UpdateDefinition.Render(new RenderArgs<BsonDocument>(BsonSerializer.LookupSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry)).AsBsonDocument;
    }

    [Fact] void should_set_the_parent_only_once() => _rendered["$set"].AsBsonDocument.ElementCount.ShouldEqual(1);
    [Fact] void should_include_the_updated_leaf() => _rendered["$set"]["info"].AsBsonDocument["name"].AsString.ShouldEqual("Again");
}
