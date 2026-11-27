// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

public class and_a_failed_partition_retry_precedes_successor_preparation : given.an_observer_retrying_a_failed_handover_partition
{
    Task Because() => RunHandoverAndRetry();

    [Fact] void should_keep_the_failed_partition_owned_by_its_episode_while_reading() => FailedDuringSuccessorRead.ShouldBeTrue();
    [Fact] void should_deliver_p_after_the_retry_read() => _delivered.ShouldContain((EventSequenceNumber)20UL);
    [Fact] void should_deliver_a_later_live_q() => _delivered.ShouldContain((EventSequenceNumber)21UL);
    [Fact] void should_clear_the_failed_episode_after_recovery() => _failedPartitionsStorage.State.Partitions.ShouldBeEmpty();
}
