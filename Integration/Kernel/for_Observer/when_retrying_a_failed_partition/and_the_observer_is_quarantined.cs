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
        public ReactorState State;
        public IEnumerable<FailedPartition> FailedPartitions;

        public Partition FailedPartition => _failedPartition.Partition;

        async Task Because()
        {
            Outcome = await RetryFailedPartition();
            RetryJobs = await GetRetryJobs();
            State = await _handler.GetState();
            FailedPartitions = await _handler.GetFailedPartitions();
        }
    }

    [Fact] void should_refuse_because_the_observer_is_quarantined() => Context.Outcome.ShouldEqual(ReactorPartitionRetryOutcome.ObserverQuarantined);
    [Fact] void should_not_create_a_retry_job() => Context.RetryJobs.ShouldBeEmpty();
    [Fact] void should_keep_the_observer_quarantined() => Context.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_keep_the_failed_partition() => Context.FailedPartitions.Select(partition => partition.Partition).ShouldContain(Context.FailedPartition);
}
