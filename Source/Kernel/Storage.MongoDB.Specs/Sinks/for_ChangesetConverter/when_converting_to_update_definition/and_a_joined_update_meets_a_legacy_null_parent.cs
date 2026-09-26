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
    List<FilterDefinition<BsonDocument>> _updateFilters;
    List<FilterDefinition<BsonDocument>> _repairFilters;
    List<UpdateDefinition<BsonDocument>> _repairUpdates;
    BsonDocument _aheadDocument;
    bool _aheadDocumentUpdated;

    void Establish()
    {
        _updateFilters = [];
        _repairFilters = [];
        _repairUpdates = [];
        _aheadDocument = new BsonDocument
        {
            ["joinProperty"] = "ciphertext",
            ["outer"] = BsonNull.Value,
            ["__lastHandledEventSequenceNumber"] = 43
        };
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
                var filter = call.ArgAt<FilterDefinition<BsonDocument>>(0);
                var update = call.ArgAt<UpdateDefinition<BsonDocument>>(1);
                if (Render(update).Contains("$unset"))
                {
                    _repairFilters.Add(filter);
                    _repairUpdates.Add(update);
                    if (Render(filter).ToJson().Contains("joinProperty") && _aheadDocument.TryGetValue("outer", out var outer) && outer == BsonNull.Value)
                    {
                        _aheadDocument.Remove("outer");
                    }
                    return Task.FromResult<UpdateResult>(new UpdateResult.Acknowledged(1, 1, null));
                }

                _updateFilters.Add(filter);
                _updateAttempts++;
                if (_updateAttempts == 1)
                {
                    return Task.FromException<UpdateResult>(failure);
                }

                // This simulated target is already at N+1. A watermark filter would miss it.
                _aheadDocumentUpdated = !Render(filter).ToJson().Contains("__lastHandledEventSequenceNumber") && !_aheadDocument.Contains("outer");
                return Task.FromResult<UpdateResult>(new UpdateResult.Acknowledged(_aheadDocumentUpdated ? 1 : 0, _aheadDocumentUpdated ? 1 : 0, null));
            });
    }

    async Task Because() => await _converter.ToUpdateDefinition(new Key("join-key", ArrayIndexers.NoIndexers), _changeset, 42UL);

    [Fact] void should_repair_the_matched_document_null_ancestors() => _repairUpdates.Count.ShouldEqual(2);
    [Fact] void should_repair_the_outer_ancestor_first() => Render(_repairUpdates[0])["$unset"].AsBsonDocument.Contains("outer").ShouldBeTrue();
    [Fact] void should_repair_only_join_targets_with_an_explicit_null_ancestor() => _repairFilters.TrueForAll(filter => Render(filter).ToJson().Contains("joinProperty") && Render(filter).ToJson().Contains("$type") && !Render(filter).ToJson().Contains("__lastHandledEventSequenceNumber")).ShouldBeTrue();
    [Fact] void should_not_query_each_document() => _collection.DidNotReceive().FindAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<FindOptions<BsonDocument>>(), Arg.Any<CancellationToken>());
    [Fact] void should_retry_the_joined_update_once() => _updateAttempts.ShouldEqual(2);
    [Fact] void should_retry_documents_even_if_their_watermark_is_ahead() => _aheadDocumentUpdated.ShouldBeTrue();
    [Fact] void should_leave_the_ahead_watermark_intact() => _aheadDocument["__lastHandledEventSequenceNumber"].AsInt32.ShouldEqual(43);
    [Fact] void should_not_add_a_watermark_filter_to_the_retry() => Render(_updateFilters[1]).ToJson().Contains("__lastHandledEventSequenceNumber").ShouldBeFalse();

    static BsonDocument Render(UpdateDefinition<BsonDocument> update) => update.Render(new RenderArgs<BsonDocument>(BsonSerializer.LookupSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry)).AsBsonDocument;
    static BsonDocument Render(FilterDefinition<BsonDocument> filter) => filter.Render(new RenderArgs<BsonDocument>(BsonSerializer.LookupSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry)).AsBsonDocument;
}
