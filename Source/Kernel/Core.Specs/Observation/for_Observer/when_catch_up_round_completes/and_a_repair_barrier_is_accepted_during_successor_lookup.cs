// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

public class and_a_repair_barrier_is_accepted_during_successor_lookup : given.an_observer_with_skip_detection
{
    readonly TaskCompletionSource _lookupStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<IImmutableList<JobState>> _lookup = new(TaskCreationOptions.RunContinuationsAsynchronously);

    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 10UL };
        await _observer.RegisterCatchingUpPartitions(new HashSet<Key> { "P" });
        await _observer.Handle("P", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 11UL)]);
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(_ =>
        {
            _lookupStarted.TrySetResult();
            return _lookup.Task;
        });
    }

    async Task Because()
    {
        var acquisition = _observer.CatchUp();
        try
        {
            await _lookupStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            await _observer.CaughtUp(JobId.NotSet, 13UL, 13UL);
        }
        finally
        {
            _lookup.TrySetResult(ImmutableList<JobState>.Empty);
            await acquisition.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
    }

    [Fact] void should_not_start_the_successor_after_the_lookup() => _jobsManager.DidNotReceive().Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
    [Fact] void should_preserve_the_newly_accepted_repair() => _stateStorage.State.PendingCatchUpRepairs!.Single().From.ShouldEqual((EventSequenceNumber)11UL);
    [Fact] void should_keep_the_frozen_successor_start() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)14UL);
}
