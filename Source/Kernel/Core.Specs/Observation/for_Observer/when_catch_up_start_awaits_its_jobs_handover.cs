// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// A zero-step job can conclude inside Start. Its handover must persist progress before Start returns, without
/// routing back into the acquisition that is still awaiting Start (#6145).
/// </summary>
public class when_catch_up_start_awaits_its_jobs_handover : given.an_observer_with_subscription
{
    readonly JobId _concludingJob = JobId.New();
    readonly JobId _replacementJob = JobId.New();
    EventSequenceNumber _positionAcknowledged;
    bool _handoverWasPersisted;
    Exception? _error;

    void Establish()
    {
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)12);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns((EventSequenceNumber)5);
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));
        _jobsManager.Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(
                async _ =>
                {
                    var writesBeforeHandover = _storageStats.Writes;
                    await _observer.CaughtUp(_concludingJob, 4);
                    _positionAcknowledged = _stateStorage.State.NextEventSequenceNumber;
                    _handoverWasPersisted = _storageStats.Writes > writesBeforeHandover;
                    return Result<JobId, StartJobError>.Success(_concludingJob);
                },
                _ => Task.FromResult(Result<JobId, StartJobError>.Success(_replacementJob)));
        _jobsManager.ClearReceivedCalls();
    }

    async Task Because() => _error = await Cratis.Specifications.Catch.Exception(() => _observer.CatchUp().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System));

    [Fact] void should_not_deadlock_the_start_that_is_waiting_for_its_handover() => _error.ShouldBeNull();
    [Fact] void should_acknowledge_the_applied_position() => _positionAcknowledged.ShouldEqual((EventSequenceNumber)5);
    [Fact] void should_persist_progress_before_acknowledging_the_handover() => _handoverWasPersisted.ShouldBeTrue();

    [Fact]
    async Task should_start_one_replacement_from_the_acknowledged_position() => await _jobsManager.Received(1)
        .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Is<CatchUpObserverRequest>(request => request.FromEventSequenceNumber == (EventSequenceNumber)5));

    [Fact]
    async Task should_start_only_the_original_and_the_replacement_job() => await _jobsManager.Received(2)
        .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
}
