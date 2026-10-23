// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.in_replay;

public class and_a_page_storage_flush_fails : given.a_performing_job_step
{
    Catch<JobStepResult> _result;
    int _flushes;

    protected override Configuration.Jobs CreateJobsConfig() => new() { StepCheckpointBatchInterval = 1 };

    void Establish() => _observerServiceClient.FlushAllFor(Arg.Any<ObserverDetails>(), Arg.Any<bool>()).Returns(_ =>
        ++_flushes == 1
            ? Task.FromResult<IReadOnlyDictionary<SiloAddress, Guid[]>>(new Dictionary<SiloAddress, Guid[]> { [SiloAddress.Zero] = [] })
            : Task.FromException<IReadOnlyDictionary<SiloAddress, Guid[]>>(new InvalidOperationException("storage unavailable")));

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_return_a_partial_failure_instead_of_throwing() => _result.Match(result => result.TryGetError(out var error) && error.HasPartialResult && !error.Cancelled, _ => false).ShouldBeTrue();
    [Fact] void should_keep_the_last_durable_checkpoint() => _performState.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_attempt_the_failed_flush_only_once() => _flushes.ShouldEqual(2);
    [Fact] void should_not_report_unconfirmed_counts() => _observer.DidNotReceive().ReportHandledEventsBatch(Arg.Any<JobStepId>(), Arg.Any<IReadOnlyList<HandledEventsReport>>());
}
