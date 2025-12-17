// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_converting_to_update_definition;

public class and_there_is_a_child_added : given.a_changeset_converter
{
    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    Key _key;
    EventSequenceNumber _eventSequenceNumber;
    UpdateDefinitionAndArrayFilters _result;
    BsonValue _expectedBsonValue;

    void Establish()
    {
        _key = new Key("some-key", ArrayIndexers.NoIndexers);
        _eventSequenceNumber = 42UL;
        _expectedBsonValue = BsonValue.Create(42UL);

        var childState = new ExpandoObject();
        var childAdded = new ChildAdded(childState, new PropertyPath("Children"), new PropertyPath("Id"), "child-key", ArrayIndexers.NoIndexers);

        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.Changes.Returns([childAdded]);

        _mongoDBConverter.ToMongoDBProperty(Arg.Any<PropertyPath>(), Arg.Any<ArrayIndexers>())
            .Returns(new MongoDBProperty("Children", []));
        _mongoDBConverter.ToBsonValue(_eventSequenceNumber)
            .Returns(_expectedBsonValue);
        _expandoObjectConverter.ToBsonDocument(Arg.Any<ExpandoObject>(), Arg.Any<NJsonSchema.JsonSchema>())
            .Returns(new BsonDocument());
    }

    async Task Because() => _result = await _converter.ToUpdateDefinition(_key, _changeset, _eventSequenceNumber);

    [Fact] void should_indicate_has_changes() => _result.hasChanges.ShouldBeTrue();
    [Fact] void should_have_update_definition() => _result.UpdateDefinition.ShouldNotBeNull();
    [Fact] void should_convert_event_sequence_number_to_bson_value() => _mongoDBConverter.Received(1).ToBsonValue(_eventSequenceNumber);
}
