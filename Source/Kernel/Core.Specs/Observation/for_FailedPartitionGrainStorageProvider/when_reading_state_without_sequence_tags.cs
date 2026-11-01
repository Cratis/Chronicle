// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_FailedPartitionGrainStorageProvider;

public class when_reading_state_without_sequence_tags : given.the_provider
{
    static readonly ObserverKey _observerKey = new("an-observer", "an-event-store", "a-namespace", "inbox-another-store");
    FailedPartitions _stored;
    GrainState<FailedPartitions> _state;

    void Establish()
    {
        _stored = new() { Partitions = [new() { ObserverId = _observerKey.ObserverId }, new() { ObserverId = _observerKey.ObserverId }] };
        _failedPartitionsStorage.GetFor(_observerKey.ObserverId).Returns(_stored);
        _state = new();
    }

    Task Because() => provider.ReadStateAsync("name", GrainId.Create("type", _observerKey.ToString()), _state);

    [Fact] void should_load_every_partition() => _state.State!.Partitions.Count().ShouldEqual(2);
    [Fact] void should_load_the_stored_state_as_is() => _state.State.ShouldBeSame(_stored);
}
