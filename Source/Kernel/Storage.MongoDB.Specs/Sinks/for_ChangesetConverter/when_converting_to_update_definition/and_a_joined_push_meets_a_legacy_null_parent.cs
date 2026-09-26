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
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_converting_to_update_definition;

public class and_a_joined_push_meets_a_legacy_null_parent : given.a_changeset_converter
{
    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    Exception? _error;
    int _updateAttempts;

    void Establish()
    {
        var joined = new Joined(
            new ExpandoObject(),
            "join-key",
            new PropertyPath("JoinProperty"),
            ArrayIndexers.NoIndexers,
            [new ChildAdded("new child", new PropertyPath("outer.info.children"), new PropertyPath("id"), "child-1", ArrayIndexers.NoIndexers)]);
        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.Changes.Returns([joined]);
        _mongoDBConverter.ToMongoDBProperty(new PropertyPath("outer.info.children"), ArrayIndexers.NoIndexers)
            .Returns(new MongoDBProperty("outer.info.children", []));
        _mongoDBConverter.ToMongoDBProperty(new PropertyPath("JoinProperty"), ArrayIndexers.NoIndexers)
            .Returns(new MongoDBProperty("joinProperty", []));
        _mongoDBConverter.ToBsonValue(Arg.Any<object?>(), Arg.Any<PropertyPath>()).Returns(BsonString.Create("join-key"));
        _mongoDBConverter.ToBsonValue(Arg.Any<EventSequenceNumber>()).Returns(BsonValue.Create(42UL));

        var writeError = (WriteError)typeof(WriteError)
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(constructor => constructor.GetParameters().Length == 4)
            .Invoke([ServerErrorCategory.Uncategorized, 28, "Cannot create field", new BsonDocument()]);
        var failure = new MongoWriteException(
            new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017))), writeError, null, null);
        _collection.UpdateManyAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _updateAttempts++;
                return Task.FromException<UpdateResult>(failure);
            });
    }

    async Task Because() => _error = await Catch.Exception(() => _converter.ToUpdateDefinition(new Key("join-key", ArrayIndexers.NoIndexers), _changeset, 42UL));

    [Fact] void should_surface_the_original_error() => _error.ShouldBeOfExactType<MongoWriteException>();
    [Fact] void should_not_retry_a_non_idempotent_push() => _updateAttempts.ShouldEqual(1);
    [Fact] void should_not_repair_any_documents_that_might_have_received_the_push() => _collection.DidNotReceive().UpdateOneAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>());
}
