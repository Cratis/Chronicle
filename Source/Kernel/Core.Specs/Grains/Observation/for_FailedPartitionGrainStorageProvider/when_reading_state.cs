// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
namespace Cratis.Chronicle.Grains.Observation.for_FailedPartitionGrainStorageProvider;

public class when_reading_state : given.the_provider
{
    static GrainId _grainId;
    static IGrainState<FailedPartitions> _state;
    static ObserverKey _observerKey;

    void Establish()
    {
        _grainId = GrainId.Create("type", "key");
        _state = new GrainState<FailedPartitions> { State = new(), ETag = "ETag", RecordExists = true };
        _observerKey = ObserverKey.Parse(_grainId.Key.ToString()!);
    }

    Task Because() => provider.ReadStateAsync("name", _grainId, _state);

    [Fact] void should_get_event_store() => storage.Received(1).GetEventStore(_observerKey.EventStore);
    [Fact] void should_get_namespace() => eventStoreStorage.Received(1).GetNamespace(_observerKey.Namespace);
    [Fact] void should_get_failed_partitions() => eventStoreNamespaceStorage.FailedPartitions.Received(1);
    [Fact] void should_get_failed_partition_state() => _failedPartitionsStorage.Received(1).GetFor(_observerKey.ObserverId);
}
