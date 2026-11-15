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

public class and_a_dictionary_key_is_removed : given.a_changeset_converter
{
    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    UpdateDefinitionAndArrayFilters _result;
    BsonDocument _update;

    void Establish()
    {
        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.Changes.Returns([
            new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [
                PropertyDifference.Removal(new PropertyPath("slots.first"), "series-1"),
                new PropertyDifference(new PropertyPath("slots.second"), null, "series-2")])]);
        _mongoDBConverter.ToMongoDBProperty(Arg.Any<PropertyPath>(), Arg.Any<ArrayIndexers>())
            .Returns(call => new MongoDBProperty(((PropertyPath)call[0]).Path, []));
        _mongoDBConverter.ToBsonValue(Arg.Any<object?>(), Arg.Any<PropertyPath>())
            .Returns(call => new BsonString((string)call[0]));
        _mongoDBConverter.ToBsonValue(Arg.Any<EventSequenceNumber>()).Returns(new BsonInt64(42));
    }

    async Task Because()
    {
        _result = await _converter.ToUpdateDefinition(new Key("key", ArrayIndexers.NoIndexers), _changeset, 42UL);
        _update = _result.UpdateDefinition.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry)).AsBsonDocument;
    }

    [Fact] void should_indicate_has_changes() => _result.hasChanges.ShouldBeTrue();
    [Fact] void should_unset_the_removed_key() => _update["$unset"].AsBsonDocument.Contains("slots.first").ShouldBeTrue();
    [Fact] void should_not_write_null_for_the_removed_key() => _update["$set"].AsBsonDocument.Contains("slots.first").ShouldBeFalse();
    [Fact] void should_set_the_other_key() => _update["$set"]["slots.second"].AsString.ShouldEqual("series-2");
}
