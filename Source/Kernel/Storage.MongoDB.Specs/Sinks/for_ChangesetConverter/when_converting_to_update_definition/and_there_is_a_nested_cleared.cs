// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_converting_to_update_definition;

public class and_there_is_a_nested_cleared : given.a_changeset_converter
{
    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    Key _key;
    EventSequenceNumber _eventSequenceNumber;
    UpdateDefinitionAndArrayFilters _result;
    BsonValue _expectedBsonValue;
    PropertyPath _nestedProperty;

    void Establish()
    {
        _key = new Key("some-key", ArrayIndexers.NoIndexers);
        _eventSequenceNumber = 42UL;
        _expectedBsonValue = BsonValue.Create(42UL);
        _nestedProperty = new PropertyPath("command");

        var nestedCleared = new NestedCleared(_nestedProperty);

        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.Changes.Returns([nestedCleared]);

        _mongoDBConverter.ToMongoDBProperty(_nestedProperty, ArrayIndexers.NoIndexers)
            .Returns(new MongoDBProperty("command", []));
        _mongoDBConverter.ToBsonValue(_eventSequenceNumber)
            .Returns(_expectedBsonValue);
    }

    async Task Because() => _result = await _converter.ToUpdateDefinition(_key, _changeset, _eventSequenceNumber);

    [Fact] void should_indicate_has_changes() => _result.hasChanges.ShouldBeTrue();
    [Fact] void should_have_update_definition() => _result.UpdateDefinition.ShouldNotBeNull();
    [Fact] void should_convert_nested_property_to_mongodb_property() => _mongoDBConverter.Received(1).ToMongoDBProperty(_nestedProperty, ArrayIndexers.NoIndexers);
    [Fact] void should_convert_event_sequence_number_to_bson_value() => _mongoDBConverter.Received(1).ToBsonValue(_eventSequenceNumber);
}
