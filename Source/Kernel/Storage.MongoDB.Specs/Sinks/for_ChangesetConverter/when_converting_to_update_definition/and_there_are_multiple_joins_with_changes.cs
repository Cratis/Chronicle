// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_converting_to_update_definition;

public class and_there_are_multiple_joins_with_changes : given.a_changeset_converter
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

        var propertyDifference1 = new PropertyDifference(new PropertyPath("Name"), "OldValue1", "NewValue1");
        var propertiesChanged1 = new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [propertyDifference1]);
        var joined1 = new Joined(new ExpandoObject(), "join-key-1", new PropertyPath("JoinProperty1"), ArrayIndexers.NoIndexers, [propertiesChanged1]);

        var propertyDifference2 = new PropertyDifference(new PropertyPath("Description"), "OldValue2", "NewValue2");
        var propertiesChanged2 = new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [propertyDifference2]);
        var joined2 = new Joined(new ExpandoObject(), "join-key-2", new PropertyPath("JoinProperty2"), ArrayIndexers.NoIndexers, [propertiesChanged2]);

        var propertyDifference3 = new PropertyDifference(new PropertyPath("Status"), "OldValue3", "NewValue3");
        var propertiesChanged3 = new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [propertyDifference3]);
        var joined3 = new Joined(new ExpandoObject(), "join-key-3", new PropertyPath("JoinProperty3"), ArrayIndexers.NoIndexers, [propertiesChanged3]);

        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.Changes.Returns([joined1, joined2, joined3]);

        _mongoDBConverter.ToMongoDBProperty(Arg.Any<PropertyPath>(), Arg.Any<ArrayIndexers>())
            .Returns(
                new MongoDBProperty("Name", []),
                new MongoDBProperty("Description", []),
                new MongoDBProperty("Status", []));
        _mongoDBConverter.ToBsonValue(Arg.Any<object?>(), Arg.Any<PropertyPath>())
            .Returns(
                BsonValue.Create("NewValue1"),
                BsonValue.Create("NewValue2"),
                BsonValue.Create("NewValue3"));
        _mongoDBConverter.ToBsonValue(_eventSequenceNumber)
            .Returns(_expectedBsonValue);
        _collection.UpdateManyAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<UpdateResult>(new UpdateResult.Acknowledged(1, 1, BsonValue.Create("some-key"))));
    }

    async Task Because() => _result = await _converter.ToUpdateDefinition(_key, _changeset, _eventSequenceNumber);

    [Fact] void should_not_indicate_has_changes_for_main_document() => _result.hasChanges.ShouldBeFalse();
    [Fact] void should_perform_update_on_all_joined_documents() => _collection.Received(3).UpdateManyAsync(
        Arg.Any<FilterDefinition<BsonDocument>>(),
        Arg.Any<UpdateDefinition<BsonDocument>>(),
        Arg.Any<UpdateOptions>(),
        Arg.Any<CancellationToken>());
}
