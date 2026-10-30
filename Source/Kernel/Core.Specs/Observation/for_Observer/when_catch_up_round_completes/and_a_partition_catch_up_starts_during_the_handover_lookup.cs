// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Properties;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

/// <summary>
/// A partition can be re-held while the handover is awaiting its job lookup. The listing predates that hold, so it
/// cannot say the partition has no job: the hold must survive (#5379).
/// </summary>
public class and_a_partition_catch_up_starts_during_the_handover_lookup : given.an_observer_with_skip_detection
{
    static readonly Key _roundPartition = new("A", ArrayIndexers.NoIndexers);
    static readonly Key _reheldPartition = new("B", ArrayIndexers.NoIndexers);

    readonly TaskCompletionSource<Catch<IImmutableList<JobState>>> _heldLookup = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _lookupEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);

    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 9UL };
        await _observer.RegisterCatchingUpPartitions([_roundPartition]);
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)12UL);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(GetSequenceNumberError.NotFound);

        // The first lookup predates the new hold; the retry sees the partition job that took it.
        var reheldPartitionJob = new JobState
        {
            Id = JobId.New(),
            Status = JobStatus.Running,
            Request = new CatchUpObserverPartitionRequest(_observerKey, ObserverType.Reactor, _reheldPartition, 5UL, [])
        };
        _jobStorage.GetJobs(Arg.Any<JobQuery>()).Returns(
            _ =>
            {
                _lookupEntered.TrySetResult();
                return _heldLookup.Task;
            },
            _ => Task.FromResult(Catch<IImmutableList<JobState>>.Success(ImmutableList.Create(reheldPartitionJob))));
    }

    async Task Because()
    {
        var handover = _observer.CaughtUp(JobId.NotSet, 12UL, 12UL);
        try
        {
            await Task.WhenAny(_lookupEntered.Task, handover).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

            // A new partition catch-up takes the hold while the lookup is still outstanding.
            EventSequenceHasNextEvent(5UL);
            await _observer.PartitionCaughtUp(_reheldPartition, 5UL, 5UL);
        }
        finally
        {
            _heldLookup.TrySetResult(Catch<IImmutableList<JobState>>.Success(ImmutableList<JobState>.Empty));
        }
        await handover.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_have_looked_up_the_outstanding_jobs() => _lookupEntered.Task.IsCompleted.ShouldBeTrue();
    [Fact] void should_have_started_the_partition_catch_up() => CheckStartedCatchupJob(5UL, _reheldPartition);
    [Fact] void should_keep_the_re_held_partition() => _stateStorage.State.CatchingUpPartitions.ShouldContain(_reheldPartition);
}
