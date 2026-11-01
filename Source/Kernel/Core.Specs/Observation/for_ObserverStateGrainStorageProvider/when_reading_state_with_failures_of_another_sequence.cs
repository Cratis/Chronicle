// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverStateGrainStorageProvider;

public class when_reading_state_with_failures_of_another_sequence : given.the_provider
{
    static readonly ObserverKey _observerKey = new("an-observer", "an-event-store", "a-namespace", EventSequenceId.Log);
    IGrainState<ObserverState> _state;

    void Establish()
    {
        _state = new GrainState<ObserverState> { State = new() };
        observerStateStorage.Get(_observerKey.ObserverId).Returns(Task.FromResult(new ObserverState { Identifier = _observerKey.ObserverId }));
        failedPartitionsStorage.GetFor(_observerKey.ObserverId).Returns(Task.FromResult(new FailedPartitions
        {
            Partitions =
            [
                new() { Partition = "parked", EventSequenceId = EventSequenceId.Inbox },
                new() { Partition = "owned", EventSequenceId = EventSequenceId.Log },
                new() { Partition = "untagged" }
            ]
        }));
    }

    Task Because() => provider.ReadStateAsync("name", GrainId.Create("type", _observerKey.ToString()), _state);

    [Fact] void should_count_only_the_failures_of_the_addressed_sequence() => _state.State.FailedPartitionCount.ShouldEqual((FailedPartitionCount)2);
    [Fact] void should_load_only_the_failures_of_the_addressed_sequence() => _state.State.FailedPartitions.Select(_ => _.Partition).ShouldContainOnly([(Key)"owned", (Key)"untagged"]);
}
