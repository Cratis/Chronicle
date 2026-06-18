// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_converting_to_update_definition;

public class and_child_identifier_change_targets_the_same_collection_as_a_child_added : given.a_changeset_converter
{
    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    Key _key;
    EventSequenceNumber _eventSequenceNumber;
    UpdateDefinitionAndArrayFilters _result;
    BsonValue _expectedBsonValue;
    PropertyPath _rolesProperty;
    PropertyPath _roleIdProperty;

    void Establish()
    {
        _key = new Key("some-key", ArrayIndexers.NoIndexers);
        _eventSequenceNumber = 42UL;
        _expectedBsonValue = BsonValue.Create(42UL);
        _rolesProperty = new PropertyPath("Roles");
        _roleIdProperty = new PropertyPath("[Roles].Id");

        dynamic childState = new ExpandoObject();
        childState.Id = "role-1";
        childState.Name = "Administrator";

        var childAdded = new ChildAdded(childState, _rolesProperty, new PropertyPath("Id"), "role-1", ArrayIndexers.NoIndexers);
        var arrayIndexers = new ArrayIndexers(
        [
            new ArrayIndexer(_rolesProperty, new PropertyPath("Id"), "role-1")
        ]);
        var propertyDifference = new PropertyDifference(_roleIdProperty, null, "role-1", arrayIndexers);
        var propertiesChanged = new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [propertyDifference]);

        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.Changes.Returns([propertiesChanged, childAdded]);

        _mongoDBConverter.ToMongoDBProperty(Arg.Any<PropertyPath>(), Arg.Any<ArrayIndexers>())
            .Returns(new MongoDBProperty("Roles", []));
        _mongoDBConverter.ToBsonValue(Arg.Any<object?>(), Arg.Any<PropertyPath>())
            .Returns(new BsonArray());
        _mongoDBConverter.ToBsonValue(_eventSequenceNumber)
            .Returns(_expectedBsonValue);
        _expandoObjectConverter.ToBsonDocument(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(new BsonDocument());
    }

    async Task Because() => _result = await _converter.ToUpdateDefinition(_key, _changeset, _eventSequenceNumber);

    [Fact] void should_indicate_has_changes() => _result.hasChanges.ShouldBeTrue();
    [Fact] void should_have_update_definition() => _result.UpdateDefinition.ShouldNotBeNull();
    [Fact] void should_not_convert_the_child_identifier_property_change_to_a_set() => _mongoDBConverter.DidNotReceive().ToBsonValue(Arg.Any<object?>(), Arg.Is<PropertyPath>(_ => _ == _roleIdProperty));
    [Fact] void should_convert_the_child_added_to_a_push() => _expandoObjectConverter.Received(1).ToBsonDocument(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>());
    [Fact] void should_convert_event_sequence_number_to_bson_value() => _mongoDBConverter.Received(1).ToBsonValue(_eventSequenceNumber);
}
