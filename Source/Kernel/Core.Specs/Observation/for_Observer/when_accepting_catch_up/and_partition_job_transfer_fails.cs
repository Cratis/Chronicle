// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Monads;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Observation.for_Observer.when_accepting_catch_up;

public class and_partition_job_transfer_fails : given.an_observer_accepting_a_catch_up_handover
{
    ObserverState _afterFailure;

    async Task Establish() => await _observer.CaughtUp(JobId.NotSet, 12UL, 12UL);

    async Task Because()
    {
        _jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>()).ThrowsAsync(new IOException("job start failed"));
        await Cratis.Specifications.Catch.Exception(_silo.RunCatchUpContinuation);
        _afterFailure = _writes.PersistedState;
        _jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));
        await _observer.CatchUp();
        await _silo.RunCatchUpContinuation();
    }

    [Fact] void should_retain_the_durable_obligation_after_failed_transfer() => _afterFailure.PendingCatchUpRepairs!.ShouldContainOnly(new PendingCatchUpRepair("B", 10UL, 13UL));
    [Fact] void should_not_rewind_the_wide_cursor() => _afterFailure.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)13UL);
    [Fact] void should_retry_without_the_original_timer() => _jobsManager.Received(2).Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Is<CatchUpObserverPartitionRequest>(_ => _.Key == "B" && _.FromSequenceNumber == (EventSequenceNumber)10UL));
}
