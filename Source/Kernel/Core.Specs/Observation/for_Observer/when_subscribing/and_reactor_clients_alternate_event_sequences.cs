// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_reactor_clients_alternate_event_sequences : given.two_clients_with_different_definitions
{
    Task Because() => RegisterAlternatingClients(ObserverType.Reactor);

    [Fact] void should_persist_the_last_registrants_sequence_each_time() => _definitions.Select(_ => _.EventSequenceId).SequenceEqual([EventSequenceId.Log, _secondSequence, EventSequenceId.Log]).ShouldBeTrue();
    [Fact] void should_persist_the_last_registrants_types_each_time() => _definitions.Select(_ => _.EventTypes.Single()).SequenceEqual([_firstType, _secondType, _firstType]).ShouldBeTrue();
    [Fact] void should_preserve_the_shared_cursor_each_time() => _states.Select(_ => _.NextEventSequenceNumber).SequenceEqual(Enumerable.Repeat((EventSequenceNumber)42UL, 3)).ShouldBeTrue();
    [Fact] void should_preserve_the_shared_last_handled_position_each_time() => _states.Select(_ => _.LastHandledEventSequenceNumber).SequenceEqual(Enumerable.Repeat((EventSequenceNumber)41UL, 3)).ShouldBeTrue();
    [Fact] void should_preserve_the_failed_partition_count_each_time() => _states.Select(_ => _.FailedPartitionCount).SequenceEqual(Enumerable.Repeat((FailedPartitionCount)1, 3)).ShouldBeTrue();
    [Fact] void should_start_one_catchup_per_sequence_then_adopt_the_first_job() => _catchupCounts.SequenceEqual([1, 2, 2]).ShouldBeTrue();
    [Fact] void should_use_the_shared_cursor_for_both_catchup_jobs() => _catchups.Select(_ => _.FromEventSequenceNumber).SequenceEqual(Enumerable.Repeat((EventSequenceNumber)42UL, 2)).ShouldBeTrue();
    [Fact] void should_keep_catchup_jobs_separate_by_sequence() => _catchups.Select(_ => _.ObserverKey.EventSequenceId).SequenceEqual([EventSequenceId.Log, _secondSequence]).ShouldBeTrue();
    [Fact] void should_keep_the_job_event_types_from_each_registration() => _catchups.Select(_ => _.EventTypes.Single()).SequenceEqual([_firstType, _secondType]).ShouldBeTrue();
    [Fact] void should_leave_both_conflicting_subscriptions_active() => new[] { _firstSubscription.IsSubscribed, _secondSubscription.IsSubscribed }.SequenceEqual([true, true]).ShouldBeTrue();
    [Fact] void should_keep_the_second_clients_types_in_its_activation() => _secondSubscription.EventTypes.ShouldContainOnly(_secondType);
    [Fact] void should_not_start_replay() => _jobsManager.DidNotReceive().Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
    [Fact] void should_defer_failed_partition_retries() => _jobsManager.DidNotReceive().Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>());
}
