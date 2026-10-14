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

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter;

public class when_a_join_bumps_the_watermark : given.a_changeset_converter
{
    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    Key _key;
    UpdateDefinitionAndArrayFilters _result;
    BsonDocument _joinUpdate;
    BsonDocument _joinFilter;
    UpdateOptions _joinOptions;

    void Establish()
    {
        _key = new Key("root-key", ArrayIndexers.NoIndexers);
        var propertiesChanged = new PropertiesChanged<ExpandoObject>(new ExpandoObject(),
            [new PropertyDifference("name", "Before", "After")]);
        var joined = new Joined(new ExpandoObject(), "joined-source", "joinKey", ArrayIndexers.NoIndexers, [propertiesChanged]);
        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.Changes.Returns([joined]);

        _mongoDBConverter.ToMongoDBProperty(Arg.Any<PropertyPath>(), Arg.Any<ArrayIndexers>())
            .Returns(call => new MongoDBProperty(call.Arg<PropertyPath>().ToString(), []));
        _mongoDBConverter.ToBsonValue(Arg.Any<object?>(), Arg.Any<PropertyPath>())
            .Returns(call => BsonValue.Create(call.ArgAt<object>(0)));
        _mongoDBConverter.ToBsonValue((EventSequenceNumber)42UL).Returns(new BsonInt64(42));
        _collection.UpdateManyAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var renderArgs = new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry);
                _joinUpdate = call.Arg<UpdateDefinition<BsonDocument>>().Render(renderArgs).AsBsonDocument;
                _joinFilter = call.Arg<FilterDefinition<BsonDocument>>().Render(renderArgs);
                _joinOptions = call.Arg<UpdateOptions>();
                return new UpdateResult.Acknowledged(2, 2, null);
            });
    }

    async Task Because() => _result = await _converter.ToUpdateDefinition(_key, _changeset, 42UL);

    [Fact] void should_update_all_matching_joined_documents() => _collection.Received(1).UpdateManyAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_filter_by_the_joined_source_not_the_root_key() => _joinFilter.ShouldEqual(new BsonDocument("joinKey", "joined-source"));
    [Fact] void should_apply_the_joined_property_change() => _joinUpdate["$set"]["name"].ShouldEqual(new BsonString("After"));
    [Fact] void should_advance_the_watermark_with_max() => _joinUpdate["$max"][WellKnownProperties.LastHandledEventSequenceNumber].ShouldEqual(new BsonInt64(42));
    [Fact] void should_not_overwrite_the_watermark_with_set() => _joinUpdate["$set"].AsBsonDocument.Contains(WellKnownProperties.LastHandledEventSequenceNumber).ShouldBeFalse();
    [Fact] void should_not_upsert_joined_documents() => _joinOptions.IsUpsert.ShouldBeFalse();
    [Fact] void should_not_report_changes_for_the_root_document() => _result.hasChanges.ShouldBeFalse();
    [Fact] void should_not_return_a_root_update() => _result.UpdateDefinition.ShouldBeNull();
}
