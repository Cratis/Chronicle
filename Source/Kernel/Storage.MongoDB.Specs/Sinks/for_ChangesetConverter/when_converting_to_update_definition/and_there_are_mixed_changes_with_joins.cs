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

public class and_there_are_mixed_changes_with_joins : given.a_changeset_converter
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

        var mainPropertyDifference = new PropertyDifference(new PropertyPath("MainProperty"), "OldMain", "NewMain");
        var mainPropertiesChanged = new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [mainPropertyDifference]);

        var joinPropertyDifference = new PropertyDifference(new PropertyPath("JoinProperty"), "OldJoin", "NewJoin");
        var joinPropertiesChanged = new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [joinPropertyDifference]);
        var joined = new Joined(new ExpandoObject(), "join-key", new PropertyPath("JoinOn"), ArrayIndexers.NoIndexers, [joinPropertiesChanged]);

        var resolvedPropertyDifference = new PropertyDifference(new PropertyPath("ResolvedProperty"), "OldResolved", "NewResolved");
        var resolvedPropertiesChanged = new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [resolvedPropertyDifference]);
        var resolvedJoin = new ResolvedJoin(new ExpandoObject(), "resolved-key", new PropertyPath("ResolvedOn"), ArrayIndexers.NoIndexers, [resolvedPropertiesChanged]);

        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.Changes.Returns([mainPropertiesChanged, joined, resolvedJoin]);

        _mongoDBConverter.ToMongoDBProperty(Arg.Any<PropertyPath>(), Arg.Any<ArrayIndexers>())
            .Returns(
                new MongoDBProperty("MainProperty", []),
                new MongoDBProperty("JoinProperty", []),
                new MongoDBProperty("ResolvedProperty", []));
        _mongoDBConverter.ToBsonValue(Arg.Any<object?>(), Arg.Any<PropertyPath>())
            .Returns(
                BsonValue.Create("NewMain"),
                BsonValue.Create("NewJoin"),
                BsonValue.Create("NewResolved"));
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

    [Fact] void should_indicate_has_changes_for_main_document() => _result.hasChanges.ShouldBeTrue();
    [Fact] void should_have_update_definition() => _result.UpdateDefinition.ShouldNotBeNull();
    [Fact] void should_perform_update_on_joined_documents() => _collection.Received(1).UpdateManyAsync(
        Arg.Any<FilterDefinition<BsonDocument>>(),
        Arg.Any<UpdateDefinition<BsonDocument>>(),
        Arg.Any<UpdateOptions>(),
        Arg.Any<CancellationToken>());
    [Fact] void should_convert_event_sequence_number_to_bson_value() => _mongoDBConverter.Received(2).ToBsonValue(_eventSequenceNumber);
}
