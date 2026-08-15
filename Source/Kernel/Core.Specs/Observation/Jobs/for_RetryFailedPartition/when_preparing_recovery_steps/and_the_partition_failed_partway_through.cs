// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_RetryFailedPartition.when_preparing_recovery_steps;

/// <summary>
/// What recovery re-delivers is the contract a client-side delivery identity rests on: the same observer, the same
/// partition, from the sequence number the partition failed on, and deliberately not flagged as a replay. Change
/// any of those and either the identity stops matching across the retry - so a consumer keyed on it repeats its
/// side effect - or the retry starts looking like a replay and OnceOnly silently swallows the work recovery exists
/// to redo.
/// </summary>
public class and_the_partition_failed_partway_through : given.a_retry_failed_partition_job
{
    IImmutableList<JobStepDetails> _steps;

    HandleEventsForPartitionArguments Arguments => (HandleEventsForPartitionArguments)_steps[0].Request;

    async Task Because() => _steps = await _job.PrepareRecoverySteps(_request);

    [Fact] void should_prepare_a_single_step() => _steps.Count.ShouldEqual(1);
    [Fact] void should_re_deliver_to_the_same_observer() => Arguments.ObserverKey.ShouldEqual(_request.ObserverKey);
    [Fact] void should_re_deliver_to_the_same_partition() => Arguments.Partition.ShouldEqual(_request.Key);
    [Fact] void should_start_from_the_sequence_number_the_partition_failed_on() => Arguments.StartEventSequenceNumber.ShouldEqual(_request.FromSequenceNumber);
    [Fact] void should_read_to_the_end_of_the_sequence() => Arguments.EndEventSequenceNumber.ShouldEqual(EventSequenceNumber.Max);
    [Fact] void should_not_present_the_redelivery_as_a_replay() => Arguments.EventObservationState.ShouldEqual(EventObservationState.None);
}
