// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_a_step_fails;

public class and_it_failed_by_timeout : given.a_replay_observer_job
{
    bool _retriesEarly;
    bool _retriesWhenExhausted;
    bool _retriesOtherFailure;

    void Because()
    {
        var timedOut = JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(
            new HandleEventsForPartitionResult(EventSequenceNumber.Unavailable)
            {
                TransientFailure = new(new("partition", Properties.ArrayIndexers.NoIndexers), 42ul, ["Response did not arrive on time"])
            },
            ["Response did not arrive on time"],
            string.Empty));
        var other = JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(
            new HandleEventsForPartitionResult(EventSequenceNumber.Unavailable),
            ["Handler threw"],
            string.Empty));
        _retriesEarly = _job.ShouldRetryStepForTesting(JobStepId.New(), timedOut, 1);
        _retriesWhenExhausted = _job.ShouldRetryStepForTesting(JobStepId.New(), timedOut, TransientTimeouts.MaximumAttempts);
        _retriesOtherFailure = _job.ShouldRetryStepForTesting(JobStepId.New(), other, 1);
    }

    [Fact] void should_run_the_step_again() => _retriesEarly.ShouldBeTrue();
    [Fact] void should_give_up_when_attempts_are_used_up() => _retriesWhenExhausted.ShouldBeFalse();
    [Fact] void should_not_retry_a_failure_that_is_not_a_timeout() => _retriesOtherFailure.ShouldBeFalse();
}
