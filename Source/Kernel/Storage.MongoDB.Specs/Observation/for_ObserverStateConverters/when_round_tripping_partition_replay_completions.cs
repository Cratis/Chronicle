// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateConverters;

public class when_round_tripping_partition_replay_completions : Specification
{
    KernelObserverState _state;
    KernelObserverState _result;
    KernelObserverState _legacy;

    void Establish() => _state = KernelObserverState.Empty with
    {
        PartitionReplayCompletions =
        [
            new("partition", 42, 50, new(Guid.NewGuid()), DateTimeOffset.UnixEpoch),
            new("other", 60, 65, null, DateTimeOffset.UnixEpoch.AddSeconds(1))
        ]
    };

    void Because()
    {
        _result = BsonSerializer.Deserialize<ObserverState>(_state.ToMongoDB().ToBson()).ToKernel();
        _legacy = BsonSerializer.Deserialize<ObserverState>(new BsonDocument()).ToKernel();
    }

    [Fact] void should_keep_all_completion_fields() => _result.PartitionReplayCompletions.ShouldEqual(_state.PartitionReplayCompletions);
    [Fact] void should_keep_missing_legacy_intents_null() => _legacy.PartitionReplayCompletions.ShouldBeNull();
}
