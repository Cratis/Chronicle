// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reflection;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_a_bulk_write;

public class and_a_resubmitted_guarded_suffix_fails : for_Sink.given.a_sink_with_gated_bulk_writes
{
    readonly List<int[]> _attempts = [];
    WriteModel<BsonDocument>[] _initial = [];
    FailedPartition[] _failures = [];

    void Establish()
    {
        _collection.BulkWriteAsync(Arg.Any<IEnumerable<WriteModel<BsonDocument>>>(), Arg.Any<BulkWriteOptions>(), Arg.Any<CancellationToken>())
            .Returns(call => Write(call.ArgAt<IEnumerable<WriteModel<BsonDocument>>>(0).ToArray()));
    }

    async Task Because()
    {
        await _sink.BeginBulk();
        for (var index = 0; index < 8; index++)
        {
            var key = new Key($"key-{(index == 6 ? 5 : index)}", ArrayIndexers.NoIndexers);
            var mode = index is 1 or 3 ? SinkWriteMode.GuardedUpsert : SinkWriteMode.Always;
            await _sink.ApplyChanges(key, Changes(index), (ulong)(index + 1), mode);
        }
        _failures = (await _sink.EndBulk()).ToArray();
    }

    [Fact] void should_resume_each_suffix_in_order() => _attempts.Select(attempt => string.Join(',', attempt))
        .ShouldEqual(["0,1,2,3,4,5,6,7", "2,3,4,5,6,7", "4,5,6,7", "7"]);
    [Fact] void should_map_the_suffix_failure_to_the_original_partition() => _failures.Select(failure => failure.EventSourceId.Value).ShouldEqual(["key-5"]);
    [Fact] void should_map_the_suffix_failure_to_the_original_sequence() => _failures.Single().EventSequenceNumber.Value.ShouldEqual(6UL);

    Task<BulkWriteResult<BsonDocument>> Write(WriteModel<BsonDocument>[] requests)
    {
        if (_initial.Length == 0)
        {
            _initial = requests;
        }
        var indexes = requests.Select(request => Array.IndexOf(_initial, request)).ToArray();
        _attempts.Add(indexes);
        var failed = Array.FindIndex(indexes, index => index is 1 or 3 or 5);
        if (failed < 0)
        {
            return Task.FromResult<BulkWriteResult<BsonDocument>>(new BulkWriteResult<BsonDocument>.Acknowledged(requests.Length, 0, 0, 0, 0, requests, []));
        }

        var message = indexes[failed] is 1 or 3 ? "E11000 duplicate key error index: _id_" : "E11000 duplicate key error index: other_1";
        var error = (BulkWriteError)Activator.CreateInstance(typeof(BulkWriteError), BindingFlags.Instance | BindingFlags.NonPublic, null, [failed, ServerErrorCategory.DuplicateKey, 11000, message, new BsonDocument()], null)!;
        var result = new BulkWriteResult<BsonDocument>.Acknowledged(requests.Length, 0, 0, 0, 0, requests[..(failed + 1)], []);
        var connection = new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017)));
        throw new MongoBulkWriteException<BsonDocument>(connection, result, [error], null, requests[(failed + 1)..]);
    }
}
