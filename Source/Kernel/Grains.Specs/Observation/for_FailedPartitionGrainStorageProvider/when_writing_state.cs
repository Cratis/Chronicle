// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
namespace Cratis.Chronicle.Grains.Observation.for_FailedPartitionGrainStorageProvider;

public class when_writing_state : given.the_provider
{
    static GrainId grainId;
    static IGrainState<FailedPartitions> state;
    static ObserverKey observerKey;

    void Establish()
    {
        grainId = GrainId.Create("type", "key");
        state = new GrainState<FailedPartitions> { State = new() { Partitions = [new() { Partition = "partition1" }, new() { Partition = "partition2" }] }, ETag = "ETag", RecordExists = true };
        observerKey = ObserverKey.Parse(grainId.Key.ToString()!);
    }

    Task Because() => provider.WriteStateAsync("name", grainId, state);

    [Fact] void should_get_event_store() => storage.Received(1).GetEventStore(observerKey.EventStore);
    [Fact] void should_get_namespace() => eventStoreStorage.Received(1).GetNamespace(observerKey.Namespace);
    [Fact] void should_get_failed_partitions() => eventStoreNamespaceStorage.FailedPartitions.Received(1);
    [Fact] void should_save_failed_partition_state() => failedPartitionsStorage.Received(1).Save(observerKey.ObserverId, state.State);
}