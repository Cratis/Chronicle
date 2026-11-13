// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using NSubstitute;

namespace Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_applying_a_direct_change_and_a_join_to_the_same_property;

[Collection(MongoDBCollection.Name)]
public class and_the_fenced_batch_aborts_between_segments(ChronicleInProcessFixture fixture) : given.a_sink_over_real_mongo(fixture)
{
    Exception? _error;
    BsonDocument _rootAfterTheAbort;
    BsonDocument _otherAfterTheAbort;
    BsonDocument _root;
    BsonDocument _other;

    async Task Because()
    {
        // A fenced batch that fits one transaction commits all of its segments or none of them. Oversized batches are split
        // into separately committed transactions, so this spec does not claim atomicity for those.
        var gates = new FenceGates(_database);
        var token = await gates.Claim("integration/models", 1);
        var probe = Substitute.For<IFencedBatchProbe>();
        var failNext = true;
        probe.BeforeCommit(Arg.Any<FenceBatchId>()).Returns(_ =>
        {
            if (!failNext)
            {
                return Task.CompletedTask;
            }

            failNext = false;
            throw new InvalidOperationException("The batch aborts after its writes ran");
        });
        using var writes = new FencedSinkWrites(new FixedFenceTokenSource(token), new FencedBatchExecutor(new MongoFenceStore(_database), FenceRetryPolicy.Default, probe));
        using var sink = CreateSink(writes);
        Change[] changes = [Direct(("count", 1)), Join(true, ("count", 2))];
        _error = await Catch.Exception(() => sink.ApplyChanges(RootKey, ChangesetOf(changes), 20UL, SinkWriteMode.Always));
        _rootAfterTheAbort = await Read("root");
        _otherAfterTheAbort = await Read("other");
        await sink.ApplyChanges(RootKey, ChangesetOf(changes), 20UL, SinkWriteMode.Always);
        _root = await Read("root");
        _other = await Read("other");
    }

    [Fact] void should_fail_the_batch() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_persist_nothing_on_the_root() => _rootAfterTheAbort["count"].ToInt32().ShouldEqual(0);
    [Fact] void should_persist_nothing_on_the_other_row() => _otherAfterTheAbort["count"].ToInt32().ShouldEqual(0);
    [Fact] void should_not_stamp_a_watermark() => _rootAfterTheAbort.Contains(Watermark).ShouldBeFalse();
    [Fact] void should_converge_the_root_on_retry() => _root["count"].ToInt32().ShouldEqual(2);
    [Fact] void should_converge_the_other_row_on_retry() => _other["count"].ToInt32().ShouldEqual(2);
}
