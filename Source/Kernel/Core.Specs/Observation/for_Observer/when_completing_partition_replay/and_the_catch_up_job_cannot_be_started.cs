// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_completing_partition_replay;

public class and_the_catch_up_job_cannot_be_started : given.a_partition_replay_covering_a_failure
{
    void Establish() => _jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>())
        .Returns(
            Task.FromException<Result<JobId, StartJobError>>(new InvalidOperationException("Injected admission failure")),
            Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));

    async Task Because()
    {
        await CompleteWithFault();
        _jobsManager.ClearReceivedCalls();
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_report_the_admission_failure() => _exception.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_keep_a_durable_hold() => _durablyHeldAfterFault.ShouldBeTrue();
    [Fact] void should_retry_the_successor_from_the_right_position() => CheckSuccessor();
    [Fact] void should_decrement_the_count_only_once() => _completionStorage.PersistedState.FailedPartitionCount.ShouldEqual(FailedPartitionCount.Zero);
}
