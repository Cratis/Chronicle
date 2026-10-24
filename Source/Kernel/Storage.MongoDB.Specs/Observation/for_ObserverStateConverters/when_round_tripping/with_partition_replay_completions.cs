// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Orleans.Jobs;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateConverters.when_round_tripping;

public class with_partition_replay_completions : Specification
{
    readonly JobId _job = JobId.New();
    readonly EventType _type = new("event", 1);
    KernelObserverState _ordinary;
    KernelObserverState _joined;
    KernelObserverState _legacy;
    KernelObserverState _cleared;

    void Because()
    {
        var state = new KernelObserverState
        {
            Identifier = "observer",
            PartitionReplayCompletions =
            [
                new("partition", _job) { LastHandledEventSequenceNumber = 42UL, LastScannedEventSequenceNumber = 48UL, ReplayedEventTypes = [_type], HandingOff = true, HandedOff = true },
                new("legacy", null) { LastHandledEventSequenceNumber = 10UL, LastScannedEventSequenceNumber = 12UL }
            ]
        };
        var document = state.ToMongoDB().ToBson();
        _ordinary = BsonSerializer.Deserialize<ObserverState>(document).ToKernel();
        _joined = BsonSerializer.Deserialize<ObserverStateWithFailedPartitions>(document).ToKernel();
        var oldShape = new KernelObserverState { Identifier = "legacy", PartitionReplayCompletions = null }.ToMongoDB().ToBsonDocument();
        _legacy = BsonSerializer.Deserialize<ObserverState>(oldShape).ToKernel();
        _cleared = BsonSerializer.Deserialize<ObserverState>((state with { PartitionReplayCompletions = [] }).ToMongoDB().ToBson()).ToKernel();
    }

    [Fact] void should_keep_the_job_identity() => _ordinary.PartitionReplayCompletions!.First().JobId.ShouldEqual(_job);
    [Fact] void should_keep_the_partition() => _ordinary.PartitionReplayCompletions!.First().Partition.ToString().ShouldEqual("partition");
    [Fact] void should_keep_the_handled_position() => _ordinary.PartitionReplayCompletions!.First().LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_keep_the_scanned_position() => _ordinary.PartitionReplayCompletions!.First().LastScannedEventSequenceNumber.ShouldEqual((EventSequenceNumber)48UL);
    [Fact] void should_keep_replayed_types() => _ordinary.PartitionReplayCompletions!.First().ReplayedEventTypes.ShouldContainOnly(_type);
    [Fact] void should_keep_the_handoff_phase() => _ordinary.PartitionReplayCompletions!.First().HandingOff.ShouldBeTrue();
    [Fact] void should_keep_the_concluded_job_exclusion() => _joined.PartitionReplayCompletions!.First().HandedOff.ShouldBeTrue();
    [Fact] void should_keep_legacy_callback_identity_absent() => _joined.PartitionReplayCompletions!.Last().JobId.ShouldBeNull();
    [Fact] void should_load_old_shape_as_empty() => _legacy.PartitionReplayCompletions!.ShouldBeEmpty();
    [Fact] void should_round_trip_a_cleared_snapshot() => _cleared.PartitionReplayCompletions!.ShouldBeEmpty();
}
