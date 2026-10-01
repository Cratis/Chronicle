// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.ReadModels;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ending_a_replay;

/// <summary>
/// A final flush that throws leaves the outcome of the replay's last writes unknown and records nothing to retry, so
/// the rebuilt collection is not promoted - but the sink still leaves replay and bulk mode, and later writes go
/// straight to the read model.
/// </summary>
public class and_the_final_flush_throws : Specification
{
    readonly Key _key = new("key", ArrayIndexers.NoIndexers);
    readonly TimeoutException _failure = new("the server did not answer");

    ISinkCollections _collections;
    IMongoCollection<BsonDocument> _collection;
    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    ReplayContext _context;
    Sink _sink;
    Exception _error;

    void Establish()
    {
        var converter = Substitute.For<IMongoDBConverter>();
        converter.ToBsonValue(Arg.Any<Key>()).Returns(info => new BsonString(info.Arg<Key>().Value.ToString()));
        _collection = Substitute.For<IMongoCollection<BsonDocument>>();
        _collection.BulkWriteAsync(Arg.Any<IEnumerable<WriteModel<BsonDocument>>>(), Arg.Any<BulkWriteOptions>(), Arg.Any<CancellationToken>())
            .Returns<Task<BulkWriteResult<BsonDocument>>>(_ => throw _failure);
        _collections = Substitute.For<ISinkCollections>();
        _collections.GetCollection().Returns(_collection);
        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.Changes.Returns([]);
        _changeset.HasBeenRemoved().Returns(true);
        _context = new(new ReadModelType("id", ReadModelGeneration.First), "things", "things-revert", DateTimeOffset.UtcNow);

        var readModel = new ReadModelDefinition(
            "id",
            "things",
            "Things",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, new JsonSchema() } },
            []);
        _sink = new Sink(readModel, converter, _collections, Substitute.For<IChangesetConverter>(), Substitute.For<IExpandoObjectConverter>(), Substitute.For<IReadModelChangeStreams>());
    }

    async Task Because()
    {
        await _sink.BeginBulk();
        await _sink.ApplyChanges(_key, _changeset, 1UL);
        _error = await Catch.Exception(() => _sink.EndReplay(_context));
        await _sink.ApplyChanges(_key, _changeset, 2UL);
    }

    [Fact] void should_fail_with_the_flush_failure() => _error.ShouldEqual(_failure);
    [Fact] void should_leave_replay_mode() => _collections.Received(1).AbandonReplay();
    [Fact] void should_not_promote_the_replay() => _collections.DidNotReceive().EndReplay(Arg.Any<ReplayContext>());
    [Fact] void should_write_later_changes_directly() => _collection.Received(1).DeleteOneAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<CancellationToken>());
}
