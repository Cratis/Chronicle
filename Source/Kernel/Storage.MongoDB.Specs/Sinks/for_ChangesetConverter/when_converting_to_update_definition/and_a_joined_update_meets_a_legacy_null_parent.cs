// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Net;
using System.Reflection;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_converting_to_update_definition;

public class and_a_joined_update_meets_a_legacy_null_parent : given.a_changeset_converter
{
    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    int _updateAttempts;
    int _repairs;
    List<FilterDefinition<BsonDocument>> _updateFilters;

    void Establish()
    {
        _updateFilters = [];
        var difference = new PropertyDifference(new PropertyPath("outer.info.name"), null, "Again");
        var joined = new Joined(
            new ExpandoObject(),
            "join-key",
            new PropertyPath("JoinProperty"),
            ArrayIndexers.NoIndexers,
            [new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [difference])]);
        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.Changes.Returns([joined]);
        _mongoDBConverter.ToMongoDBProperty(new PropertyPath("outer.info.name"), ArrayIndexers.NoIndexers)
            .Returns(new MongoDBProperty("outer.info.name", []));
        _mongoDBConverter.ToMongoDBProperty(new PropertyPath("JoinProperty"), ArrayIndexers.NoIndexers)
            .Returns(new MongoDBProperty("joinProperty", []));
        _mongoDBConverter.ToBsonValue(Arg.Any<object?>(), Arg.Any<PropertyPath>()).Returns(BsonString.Create("ciphertext"));
        _mongoDBConverter.ToBsonValue(Arg.Any<EventSequenceNumber>()).Returns(BsonValue.Create(42UL));

        var writeError = (WriteError)typeof(WriteError)
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(constructor => constructor.GetParameters().Length == 4)
            .Invoke([ServerErrorCategory.Uncategorized, 28, "Cannot create field", new BsonDocument()]);
        var failure = new MongoWriteException(
            new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017))), writeError, null, null);
        _collection.UpdateManyAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _updateFilters.Add(call.ArgAt<FilterDefinition<BsonDocument>>(0));
                _updateAttempts++;
                return _updateAttempts == 1
                    ? Task.FromException<UpdateResult>(failure)
                    : Task.FromResult<UpdateResult>(new UpdateResult.Acknowledged(1, 1, null));
            });
        _collection.UpdateOneAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _repairs++;
                return Task.FromResult<UpdateResult>(new UpdateResult.Acknowledged(1, 1, null));
            });
        var cursor = Substitute.For<IAsyncCursor<BsonDocument>>();
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true), Task.FromResult(false));
        cursor.Current.Returns([new BsonDocument("_id", "probe-1")]);
        _collection.FindAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<FindOptions<BsonDocument>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(cursor));
    }

    async Task Because() => await _converter.ToUpdateDefinition(new Key("join-key", ArrayIndexers.NoIndexers), _changeset, 42UL);

    [Fact] void should_repair_the_matched_document_null_ancestors() => _repairs.ShouldEqual(2);
    [Fact] void should_retry_the_joined_update_once() => _updateAttempts.ShouldEqual(2);
    [Fact] void should_exclude_documents_already_written_before_the_failure() => _updateFilters[1]
        .Render(new RenderArgs<BsonDocument>(BsonSerializer.LookupSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry))
        .ToJson().Contains("__lastHandledEventSequenceNumber").ShouldBeTrue();
}
