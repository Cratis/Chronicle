// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Net;
using System.Reflection;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_a_bulk_write.given;

public class an_ordered_bulk_write : Specification
{
    protected const int OperationCount = 1000;
    protected int[] _failureIndexes = [];
    protected bool _writeConcernFailure;
    protected List<int[]> _attempts = [];
    protected FailedPartition[] _failedPartitions = [];

    Sink _sink;
    IMongoCollection<BsonDocument> _collection;
    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    WriteModel<BsonDocument>[] _initialOperations = [];
    readonly ConnectionId _connectionId = new(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017)));

    void Establish()
    {
        var converter = Substitute.For<IMongoDBConverter>();
        converter.ToBsonValue(Arg.Any<Key>()).Returns(info => new BsonString(info.Arg<Key>().Value.ToString()));
        var collections = Substitute.For<ISinkCollections>();
        _collection = Substitute.For<IMongoCollection<BsonDocument>>();
        collections.GetCollection().Returns(_collection);
        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.Changes.Returns([]);
        _changeset.HasBeenRemoved().Returns(true);

        var readModel = new ReadModelDefinition(
            "id",
            "Bulk",
            "bulk",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, new JsonSchema() } },
            []);
        _sink = new Sink(readModel, converter, collections, Substitute.For<IChangesetConverter>(), Substitute.For<IExpandoObjectConverter>());
        _collection.BulkWriteAsync(Arg.Any<IEnumerable<WriteModel<BsonDocument>>>(), Arg.Any<BulkWriteOptions>(), Arg.Any<CancellationToken>())
            .Returns(info => SimulateWrite(info.ArgAt<IEnumerable<WriteModel<BsonDocument>>>(0)));
    }

    protected async Task Flush()
    {
        await _sink.BeginBulk();
        for (var index = 0; index < OperationCount; index++)
        {
            var key = KeyFor(index);
            var failed = await _sink.ApplyChanges(key, _changeset, (ulong)(index + 1));
            if (index == OperationCount - 1)
            {
                _failedPartitions = failed.ToArray();
            }
        }
    }

    protected virtual Key KeyFor(int index) => new($"key-{index}", ArrayIndexers.NoIndexers);

    protected bool Sent(params (int Start, int End)[] ranges) =>
        _attempts.Count == ranges.Length &&
        _attempts.Select((attempt, index) => attempt.SequenceEqual(Enumerable.Range(ranges[index].Start, ranges[index].End - ranges[index].Start)))
            .All(matches => matches);

    protected bool Failed(params int[] indexes) =>
        _failedPartitions.Select(failed => (failed.EventSourceId.Value, failed.EventSequenceNumber.Value))
            .SequenceEqual(indexes.Select(index => ((object)$"key-{index}", (ulong)(index + 1))));

    Task<BulkWriteResult<BsonDocument>> SimulateWrite(IEnumerable<WriteModel<BsonDocument>> operations)
    {
        var attempt = operations.ToArray();
        if (_initialOperations.Length == 0)
        {
            _initialOperations = attempt;
        }

        var ids = attempt.Select(operation => Array.IndexOf(_initialOperations, operation)).ToArray();
        _attempts.Add(ids);
        var failureIndex = Array.FindIndex(ids, _failureIndexes.Contains);
        if (_writeConcernFailure || failureIndex >= 0)
        {
            var processed = _writeConcernFailure ? attempt : attempt[..(failureIndex + 1)];
            var result = new BulkWriteResult<BsonDocument>.Acknowledged(attempt.Length, 0, 0, 0, 0, processed, []);
            var error = failureIndex >= 0 ? new[] { Create<BulkWriteError>(failureIndex, ServerErrorCategory.DuplicateKey, 11000, "duplicate", new BsonDocument()) } : [];
            var concern = _writeConcernFailure ? Create<WriteConcernError>(64, "WriteConcernFailed", "write concern failed", new BsonDocument(), Array.Empty<string>()) : null;
            throw new MongoBulkWriteException<BsonDocument>(_connectionId, result, error, concern, attempt[processed.Length..]);
        }

        return Task.FromResult<BulkWriteResult<BsonDocument>>(
            new BulkWriteResult<BsonDocument>.Acknowledged(attempt.Length, 0, 0, 0, 0, attempt, []));
    }

    static T Create<T>(params object[] arguments) =>
        (T)Activator.CreateInstance(typeof(T), BindingFlags.Instance | BindingFlags.NonPublic, null, arguments, null)!;
}
