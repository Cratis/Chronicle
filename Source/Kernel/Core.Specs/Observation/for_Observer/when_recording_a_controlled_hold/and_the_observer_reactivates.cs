// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_recording_a_controlled_hold;

public class and_the_observer_reactivates : given.a_controlled_observer
{
    FailedPartitionId _episode;

    async Task Establish()
    {
        await _observer.PartitionHeld(partition, new(10UL, FailedPartitionRecoveryMode.Live, null), added_type.Id);
        _episode = _failedPartitionsState.Partitions.Single().Id;
    }

    async Task Because() => await Reactivate();

    [Fact] void should_keep_the_failure_identity() => _failedPartitionsStorage.State.Partitions.Single().Id.ShouldEqual(_episode);
    [Fact] void should_keep_the_waited_for_type() => _failedPartitionsStorage.State.Partitions.Single().Hold!.AwaitedEventType.ShouldEqual(added_type.Id);
    [Fact] void should_keep_the_live_resume_position() => _failedPartitionsStorage.State.Partitions.Single().RecoveryCursor!.Position.ShouldEqual((EventSequenceNumber)10UL);
    [Fact] void should_not_create_an_attempt() => _failedPartitionsStorage.State.Partitions.Single().Attempts.ShouldBeEmpty();
}
