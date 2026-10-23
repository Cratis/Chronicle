// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_FailedPartitionGrainStorageProvider;

public class when_writing_state_for_a_key_on_the_event_sequence_of_the_definition : given.the_provider
{
    static readonly ObserverKey _observerKey = new("an-observer", "an-event-store", "a-namespace", "inbox-another-store");
    static GrainState<FailedPartitions> _state;

    void Establish()
    {
        _observerDefinitionsStorage.Get(_observerKey.ObserverId).Returns(ObserverDefinition.Empty with { EventSequenceId = _observerKey.EventSequenceId });
        _state = new() { State = new() { Partitions = [new() { Partition = "partition1" }] } };
    }

    Task Because() => provider.WriteStateAsync("name", GrainId.Create("type", _observerKey.ToString()), _state);

    [Fact] void should_save_the_failed_partitions() => _failedPartitionsStorage.Received(1).Save(_observerKey.ObserverId, _state.State);
}
