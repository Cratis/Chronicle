// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

public class and_a_failed_partition_retry_precedes_successor_preparation : given.an_observer_retrying_a_failed_handover_partition
{
    [Fact] void should_not_start_retry_while_another_range_is_outstanding() => StartedBeforeTheOtherRepairFinished.ShouldBeFalse();
    [Fact] void should_keep_the_failed_partition_owned_by_its_episode_while_reading() => FailedDuringSuccessorRead.ShouldBeTrue();
    [Fact] void should_allow_healthy_work_to_progress() => HealthyProgress.ShouldBeTrue();
    [Fact] void should_eventually_start_recovery_at_the_first_unhandled_position() => RetryFrom.ShouldEqual((EventSequenceNumber)12UL);
    [Fact] void should_deliver_p_eighteen_only_once() => _delivered.Count(_ => _ == (EventSequenceNumber)18UL).ShouldEqual(1);
    [Fact] void should_clear_the_failed_episode_after_recovery() => _failedPartitionsStorage.State.Partitions.ShouldBeEmpty();
}
