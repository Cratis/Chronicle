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

public class and_the_parent_was_missing_before_nested_properties_changed : given.a_changeset_converter
{
    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    BsonDocument _rendered;

    void Establish()
    {
        var parent = new PropertyPath("info");
        dynamic info = new ExpandoObject();
        info.name = "Again";
        dynamic state = new ExpandoObject();
        state.info = info;
        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.InitialState.Returns(new ExpandoObject());
        _changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>((ExpandoObject)state,
            [new PropertyDifference(new PropertyPath("info.name"), null, "Again")])]);
        _mongoDBConverter.ToMongoDBProperty(parent, ArrayIndexers.NoIndexers).Returns(new MongoDBProperty("info", []));
        _mongoDBConverter.ToBsonValue(Arg.Any<object?>(), parent).Returns(new BsonDocument("name", "Again"));
        _mongoDBConverter.ToBsonValue(Arg.Any<EventSequenceNumber>()).Returns(BsonValue.Create(42UL));
    }

    async Task Because()
    {
        var result = await _converter.ToUpdateDefinition(new Key("probe-1", ArrayIndexers.NoIndexers), _changeset, 42UL);
        _rendered = result.UpdateDefinition.Render(new RenderArgs<BsonDocument>(BsonSerializer.LookupSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry)).AsBsonDocument;
    }

    [Fact] void should_set_the_whole_missing_parent() => _rendered["$set"]["info"].AsBsonDocument["name"].AsString.ShouldEqual("Again");
    [Fact] void should_not_set_the_leaf() => _rendered["$set"].AsBsonDocument.ElementCount.ShouldEqual(1);
}
