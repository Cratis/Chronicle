// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateConverters;

public class when_round_tripping_partition_repairs : Specification
{
    static readonly PartitionRepairState _repair = new(
        "partition",
        new(42, true, [new("chosen", EventTypeGeneration.First)]),
        12,
        null,
        new(new(Guid.NewGuid()), new(4)),
        [new(EventSequenceNumber.Max, false, [])],
        CountsPrepared: true);
    KernelObserverState _result;
    KernelObserverState _legacy;
    KernelObserverState _cleared;
    KernelObserverState _joined;

    void Because()
    {
        var state = KernelObserverState.Empty with { Identifier = "observer", PartitionRepairs = [_repair] };
        var bson = state.ToMongoDB().ToBsonDocument();
        _result = BsonSerializer.Deserialize<ObserverState>(bson).ToKernel();
        _joined = BsonSerializer.Deserialize<ObserverStateWithFailedPartitions>(bson).ToKernel();
        bson.Remove(nameof(ObserverState.PartitionRepairs));
        _legacy = BsonSerializer.Deserialize<ObserverState>(bson).ToKernel();
        _cleared = BsonSerializer.Deserialize<ObserverState>((state with { PartitionRepairs = null }).ToMongoDB().ToBson()).ToKernel();
    }

    [Fact] void should_map_the_joined_read() => _joined.PartitionRepairs!.Single().Retiring.ShouldEqual(_repair.Retiring);
    [Fact] void should_keep_the_held_back_position() => _result.PartitionRepairs!.Single().HeldBackFrom.ShouldEqual(_repair.HeldBackFrom);
    [Fact] void should_keep_the_exact_retiring_job_and_epoch() => _result.PartitionRepairs!.Single().Retiring.ShouldEqual(_repair.Retiring);
    [Fact] void should_keep_coverage_and_generation() => _result.PartitionRepairs!.Single().Head.EventTypes.ShouldEqual(_repair.Head.EventTypes);
    [Fact] void should_keep_different_waiting_coverage() => _result.PartitionRepairs!.Single().Waiting.Single().Through.ShouldEqual(EventSequenceNumber.Max);
    [Fact] void should_treat_a_legacy_row_as_having_no_repairs() => _legacy.PartitionRepairs.ShouldBeNull();
    [Fact] void should_clear_the_repairs() => _cleared.PartitionRepairs.ShouldBeNull();
}
