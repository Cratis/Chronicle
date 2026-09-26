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

public class and_the_parent_was_null_before_nested_properties_changed : given.a_changeset_converter
{
    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    BsonDocument _rendered;

    void Establish()
    {
        var leaf = new PropertyPath("outer.info.name");
        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.InitialState.Returns(new ExpandoObject());
        _changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(),
            [new PropertyDifference(leaf, null, "Again")])]);
        _mongoDBConverter.ToMongoDBProperty(leaf, ArrayIndexers.NoIndexers).Returns(new MongoDBProperty("outer.info.name", []));
        _mongoDBConverter.ToBsonValue("Again", leaf).Returns(BsonString.Create("ciphertext"));
    }

    async Task Because()
    {
        var result = await _converter.ToUpdateDefinition(new Key("probe-1", ArrayIndexers.NoIndexers), _changeset, EventSequenceNumber.Unavailable);
        _rendered = result.UpdateDefinition.Render(new RenderArgs<BsonDocument>(BsonSerializer.LookupSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry)).AsBsonDocument;
    }

    [Fact] void should_set_only_the_dotted_leaf_to_the_converted_value() => _rendered["$set"]["outer.info.name"].AsString.ShouldEqual("ciphertext");
    [Fact] void should_not_set_the_whole_parent() => _rendered["$set"].AsBsonDocument.Contains("outer.info").ShouldBeFalse();
}
