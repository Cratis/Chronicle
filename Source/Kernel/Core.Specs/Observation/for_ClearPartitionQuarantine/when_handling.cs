// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_ClearPartitionQuarantine;

public class when_handling : given.a_grain_factory_accepting_operator_commands
{
    ClearPartitionQuarantineResult _result;

    async Task Because() => _result = await new ClearPartitionQuarantine("some-event-store", "some-namespace", "some-observer", string.Empty, "some-partition", true)
        .Handle(_grainFactory, _storage)
        .WaitAsync(TimeSpan.FromSeconds(5));

    [Fact] void should_accept_clearing_the_quarantine_as_a_job_for_the_partition() => _jobsManager.Received(1).Start<IObserverOperatorCommandJob, ObserverOperatorCommandRequest>(
        new ObserverOperatorCommandRequest(_key, ObserverOperatorCommand.ClearPartitionQuarantine, "some-partition", true));
    [Fact] void should_report_that_it_was_accepted() => _result.Outcome.ShouldEqual(ClearPartitionQuarantineOutcome.Accepted);
    [Fact] void should_report_that_the_retry_was_accepted() => _result.RetryOutcome.ShouldEqual(PartitionRecoveryOutcome.Accepted);
    [Fact] void should_not_call_the_observer() => _observer.DidNotReceive().ClearPartitionQuarantine(Arg.Any<Concepts.Keys.Key>(), Arg.Any<bool>());
}
