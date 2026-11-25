// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_RetryPartition;

public class when_handling : given.a_grain_factory_accepting_operator_commands
{
    PartitionRecoveryOutcome _outcome;

    async Task Because() => _outcome = await new RetryPartition("some-event-store", "some-namespace", "some-observer", string.Empty, "some-partition")
        .Handle(_grainFactory, _storage)
        .WaitAsync(TimeSpan.FromSeconds(5));

    [Fact] void should_accept_the_retry_as_a_job_for_the_partition() => _jobsManager.Received(1).Start<IObserverOperatorCommandJob, ObserverOperatorCommandRequest>(
        new ObserverOperatorCommandRequest(_key, ObserverOperatorCommand.RetryPartition, "some-partition"));
    [Fact] void should_report_that_it_was_accepted() => _outcome.ShouldEqual(PartitionRecoveryOutcome.Accepted);
    [Fact] void should_not_call_the_observer() => _observer.DidNotReceive().TryStartRecoverJobForFailedPartition(Arg.Any<Concepts.Keys.Key>());
}
