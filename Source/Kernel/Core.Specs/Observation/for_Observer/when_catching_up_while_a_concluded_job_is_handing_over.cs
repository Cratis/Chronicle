// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Properties;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// A concluded job stops counting as an owner before its handover applies the position it reached, as the handover
/// awaits a job lookup in between. A catch-up in that window found no owner and started over everything the concluded
/// job had handled, delivering it twice (#6145).
/// </summary>
public class when_catching_up_while_a_concluded_job_is_handing_over : given.an_observer_with_subscription
{
    static readonly JobId _concludedJob = JobId.New();
    static readonly Key _partition = new("A", ArrayIndexers.NoIndexers);

    readonly TaskCompletionSource<Catch<IImmutableList<JobState>>> _heldLookup = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _lookupEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);

    async Task Establish()
    {
        await _observer.RegisterCatchingUpPartitions([_partition]);

        var concluded = new JobState
        {
            Id = _concludedJob,
            Status = JobStatus.Running,
            Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, [])
        };
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(_ => Task.FromResult<IImmutableList<JobState>>(ImmutableList.Create(concluded)));
        _jobsManager
            .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(_ => Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));

        // The handover's lookup of the partition holds is held, so the catch-up runs while the position is not yet applied.
        _jobStorage.GetJobs(Arg.Any<JobQuery>()).Returns(
            _ =>
            {
                _lookupEntered.TrySetResult();
                return _heldLookup.Task;
            },
            _ => Task.FromResult(Catch<IImmutableList<JobState>>.Success(ImmutableList<JobState>.Empty)));

        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)10UL);
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(GetSequenceNumberError.NotFound);
    }

    async Task Because()
    {
        var handover = _observer.CaughtUp(_concludedJob, 10UL, 10UL);
        var catchUp = Task.CompletedTask;
        try
        {
            await Task.WhenAny(_lookupEntered.Task, handover).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            catchUp = _observer.CatchUp();
        }
        finally
        {
            _heldLookup.TrySetResult(Catch<IImmutableList<JobState>>.Success(ImmutableList<JobState>.Empty));
        }

        await Task.WhenAll(handover, catchUp).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_have_held_the_handover_lookup() => _lookupEntered.Task.IsCompleted.ShouldBeTrue();

    [Fact]
    async Task should_not_start_a_job_over_what_the_concluded_job_handled() =>
        await _jobsManager.DidNotReceive().Start<ICatchUpObserver, CatchUpObserverRequest>(
            Arg.Is<CatchUpObserverRequest>(_ => _.FromEventSequenceNumber <= 10UL));
}
