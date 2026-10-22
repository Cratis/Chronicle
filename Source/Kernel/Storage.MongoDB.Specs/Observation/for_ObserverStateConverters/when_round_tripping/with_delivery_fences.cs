// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateConverters.when_round_tripping;

public class with_delivery_fences : Specification
{
    readonly DateTimeOffset _releaseBy = DateTimeOffset.UnixEpoch.AddSeconds(330);
    readonly DeliveryFenceTarget _target = new("type", "key", "A", "silo");
    KernelObserverState _ordinary;
    KernelObserverState _joined;
    KernelObserverState _legacy;
    KernelObserverState _released;
    BsonDocument _legacyDocument;

    void Because()
    {
        var state = new KernelObserverState
        {
            Identifier = "observer",
            DeliveryFences = [new("partition", _releaseBy, [_target]), new("conservative", _releaseBy, [])]
        };
        var document = state.ToMongoDB().ToBson();
        _ordinary = BsonSerializer.Deserialize<ObserverState>(document).ToKernel();
        _joined = BsonSerializer.Deserialize<ObserverStateWithFailedPartitions>(document).ToKernel();
        _legacyDocument = new KernelObserverState { Identifier = "legacy" }.ToMongoDB().ToBsonDocument();
        _legacy = BsonSerializer.Deserialize<ObserverState>(_legacyDocument).ToKernel();
        _released = BsonSerializer.Deserialize<ObserverState>((state with { DeliveryFences = [] }).ToMongoDB().ToBson()).ToKernel();
    }

    [Fact] void should_keep_the_original_partition() => _ordinary.DeliveryFences.First().Partition.ToString().ShouldEqual("partition");
    [Fact] void should_keep_the_deadline() => _ordinary.DeliveryFences.First().ReleaseBy.ShouldEqual(_releaseBy);
    [Fact] void should_keep_the_original_target() => _ordinary.DeliveryFences.First().Targets.ShouldContainOnly(_target);
    [Fact] void should_keep_a_conservative_hold() => _ordinary.DeliveryFences.Last().Targets.ShouldBeEmpty();
    [Fact] void should_keep_the_targets_on_the_joined_read() => _joined.DeliveryFences.First().Targets.ShouldContainOnly(_target);
    [Fact] void should_keep_the_deadline_on_the_joined_read() => _joined.DeliveryFences.First().ReleaseBy.ShouldEqual(_releaseBy);
    [Fact] void should_not_write_a_snapshot_for_legacy_state() => _legacyDocument.Contains("deliveryFences").ShouldBeFalse();
    [Fact] void should_load_legacy_state_without_a_fence_snapshot() => _legacy.DeliveryFences.ShouldBeNull();
    [Fact] void should_distinguish_a_drained_snapshot_from_legacy_state() => _released.DeliveryFences.ShouldNotBeNull();
    [Fact] void should_round_trip_an_empty_snapshot() => _released.DeliveryFences.ShouldBeEmpty();
}
