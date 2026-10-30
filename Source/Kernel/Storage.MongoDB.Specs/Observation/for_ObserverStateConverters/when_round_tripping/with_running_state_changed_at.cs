// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization;

using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateConverters.when_round_tripping;

public class with_running_state_changed_at : Specification
{
    readonly DateTimeOffset _timestamp = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    KernelObserverState _saved;
    KernelObserverState _joined;

    void Because()
    {
        var bson = new KernelObserverState { Identifier = "observer", RunningStateChangedAt = _timestamp }.ToMongoDB().ToBson();
        _saved = BsonSerializer.Deserialize<ObserverState>(bson).ToKernel();
        _joined = BsonSerializer.Deserialize<ObserverStateWithFailedPartitions>(bson).ToKernel();
    }

    [Fact] void should_keep_the_timestamp() => _saved.RunningStateChangedAt.ShouldEqual(_timestamp);
    [Fact] void should_keep_the_timestamp_on_the_joined_read() => _joined.RunningStateChangedAt.ShouldEqual(_timestamp);
}
