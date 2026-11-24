// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_CatchUpAdmission;

public class when_observers_ask_at_the_same_time_and_the_last_to_ask_is_decided_first : given.a_catch_up_admission
{
    static readonly string[] _observers = ["first", "second", "third", "fourth"];

    Task<IDisposable?>[] _decisions;
    IDisposable?[] _leases;

    async Task Because()
    {
        // Every observer asks while no catch-up job is running, and each job listing completes only when released.
        var listings = _observers.Select(_ => new TaskCompletionSource<Catch<IImmutableList<JobState>>>()).ToArray();
        var next = 0;
        _jobStorage.GetJobs(Arg.Any<JobQuery>()).Returns(_ => listings[next++].Task);

        _decisions = _observers.Select(observer => _admission.TryAdmit(KeyOf(observer), 2, 0)).ToArray();

        // The listings come back in the opposite order from the one the observers asked in.
        foreach (var listing in listings.Reverse())
        {
            _time.Advance(TimeSpan.FromMilliseconds(1));
            listing.SetResult(Catch<IImmutableList<JobState>>.Success(ImmutableList<JobState>.Empty));
        }

        _leases = await Task.WhenAll(_decisions);
    }

    [Fact] void should_admit_as_many_observers_as_there_are_places() => _leases.Count(lease => lease is not null).ShouldEqual(2);
}
