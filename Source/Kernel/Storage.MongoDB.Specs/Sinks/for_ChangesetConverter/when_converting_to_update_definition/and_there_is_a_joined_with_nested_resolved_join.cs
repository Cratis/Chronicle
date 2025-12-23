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

public class and_there_is_a_joined_with_nested_resolved_join : given.a_changeset_converter
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

        var innerPropertyDifference = new PropertyDifference(new PropertyPath("InnerProperty"), "OldInner", "NewInner");
        var innerPropertiesChanged = new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [innerPropertyDifference]);
        var resolvedJoin = new ResolvedJoin(new ExpandoObject(), "resolved-join-key", new PropertyPath("ResolvedJoinProperty"), ArrayIndexers.NoIndexers, [innerPropertiesChanged]);

        var outerPropertyDifference = new PropertyDifference(new PropertyPath("OuterProperty"), "OldOuter", "NewOuter");
        var outerPropertiesChanged = new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [outerPropertyDifference]);
        var joined = new Joined(new ExpandoObject(), "join-key", new PropertyPath("JoinProperty"), ArrayIndexers.NoIndexers, [outerPropertiesChanged, resolvedJoin]);

        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.Changes.Returns([joined]);

        _mongoDBConverter.ToMongoDBProperty(Arg.Any<PropertyPath>(), Arg.Any<ArrayIndexers>())
            .Returns(
                new MongoDBProperty("OuterProperty", []),
                new MongoDBProperty("InnerProperty", []));
        _mongoDBConverter.ToBsonValue(Arg.Any<object?>(), Arg.Any<PropertyPath>())
            .Returns(
                BsonValue.Create("NewOuter"),
                BsonValue.Create("NewInner"));
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
    [Fact] void should_perform_update_on_joined_documents() => _collection.Received(1).UpdateManyAsync(
        Arg.Any<FilterDefinition<BsonDocument>>(),
        Arg.Any<UpdateDefinition<BsonDocument>>(),
        Arg.Any<UpdateOptions>(),
        Arg.Any<CancellationToken>());
}
