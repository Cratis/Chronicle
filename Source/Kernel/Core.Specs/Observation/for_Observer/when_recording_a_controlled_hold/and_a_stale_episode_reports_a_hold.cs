// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_recording_a_controlled_hold;

public class and_a_stale_episode_reports_a_hold : given.a_controlled_observer
{
    FailedPartitionId _episode;

    async Task Establish()
    {
        await _observer.PartitionHeld(partition, new(10UL, FailedPartitionRecoveryMode.Live, null), added_type.Id);
        _episode = _failedPartitionsState.Partitions.Single().Id;
    }

    Task Because() => _observer.PartitionHeld(partition, new(20UL, FailedPartitionRecoveryMode.Replay, 30UL), event_type.Id, FailedPartitionId.New());

    [Fact] void should_keep_the_original_episode() => _failedPartitionsState.Partitions.Single().Id.ShouldEqual(_episode);
    [Fact] void should_not_replace_the_current_cursor() => _failedPartitionsState.Partitions.Single().RecoveryCursor!.Position.ShouldEqual((EventSequenceNumber)10UL);
    [Fact] void should_not_record_an_attempt() => _failedPartitionsState.Partitions.Single().Attempts.ShouldBeEmpty();
}
