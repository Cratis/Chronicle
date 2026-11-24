// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;

using context = Cratis.Chronicle.Kernel.Integration.for_Observer.when_retrying_a_failed_partition.and_the_observer_is_quarantined.context;

namespace Cratis.Chronicle.Kernel.Integration.for_Observer.when_retrying_a_failed_partition;

[Collection(given.QuarantineRecoveryCollection.Name)]
public class and_the_observer_is_quarantined(context context) : Given<context>(context)
{
    public class context(given.QuarantineRecoveryFixture fixture) : given.a_quarantined_reactor(fixture)
    {
        public ReactorPartitionRetryOutcome Outcome;
        public IEnumerable<Job> RetryJobs;
        public Job? CommandJob;
        public ReactorState State;
        public IEnumerable<FailedPartition> FailedPartitions;

        public Partition FailedPartition => _failedPartition.Partition;

        async Task Because()
        {
            Outcome = await RetryFailedPartition();
            CommandJob = await WaitForOperatorCommandJobToFinish(TimeSpan.FromSeconds(30));
            RetryJobs = await GetRetryJobs();
            State = await _handler.GetState();
            FailedPartitions = await _handler.GetFailedPartitions();
        }
    }

    [Fact] void should_accept_the_retry_as_a_job() => Context.Outcome.ShouldEqual(ReactorPartitionRetryOutcome.Accepted);
    [Fact] void should_fail_the_job_because_the_observer_is_quarantined() => Context.CommandJob?.Status.ShouldEqual(JobStatus.CompletedWithFailures);
    [Fact] void should_not_create_a_retry_job() => Context.RetryJobs.ShouldBeEmpty();
    [Fact] void should_keep_the_observer_quarantined() => Context.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_keep_the_failed_partition() => Context.FailedPartitions.Select(partition => partition.Partition).ShouldContain(Context.FailedPartition);
}
