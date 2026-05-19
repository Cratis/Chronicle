// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverStateGrainStorageProvider;

public class when_reading_state : given.the_provider
{
    static GrainId _grainId;
    static IGrainState<ObserverState> _state;
    static ObserverKey _observerKey;
    static FailedPartitions _failedPartitions;

    void Establish()
    {
        _grainId = GrainId.Create("type", "key");
        _state = new GrainState<ObserverState> { State = new(), ETag = "ETag", RecordExists = true };
        _observerKey = ObserverKey.Parse(_grainId.Key.ToString());
        _failedPartitions = new()
        {
            Partitions =
            [
                new() { Partition = "partition-1" },
                new() { Partition = "partition-2" }
            ]
        };

        observerStateStorage.Get(_observerKey.ObserverId).Returns(Task.FromResult(new ObserverState { FailedPartitionCount = 42 }));
        failedPartitionsStorage.GetFor(_observerKey.ObserverId).Returns(Task.FromResult(_failedPartitions));
    }

    Task Because() => provider.ReadStateAsync("name", _grainId, _state);

    [Fact] void should_get_event_store() => storage.Received(1).GetEventStore(_observerKey.EventStore);
    [Fact] void should_get_namespace() => eventStoreStorage.Received(1).GetNamespace(_observerKey.Namespace);
    [Fact] void should_get_observer_state() => observerStateStorage.Received(1).Get(_observerKey.ObserverId);
    [Fact] void should_get_failed_partitions() => eventStoreNamespaceStorage.FailedPartitions.Received(1);
    [Fact] void should_get_actual_failed_partitions() => failedPartitionsStorage.Received(1).GetFor(_observerKey.ObserverId);
    [Fact] void should_set_identifier() => _state.State.Identifier.ShouldEqual(_observerKey.ObserverId);
    [Fact] void should_set_failed_partition_count_to_actual_count() => _state.State.FailedPartitionCount.ShouldEqual((FailedPartitionCount)2);
    [Fact] void should_set_failed_partitions_to_actual_partitions() => _state.State.FailedPartitions.Select(_ => _.Partition).ShouldContainOnly([(Key)"partition-1", (Key)"partition-2"]);
}
